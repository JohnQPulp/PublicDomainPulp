using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Pulp.Pulpifier;

namespace Pulp.PublicDomainPulp;

public readonly record struct VisualNovel(string DirName, Metadata Metadata, byte[] Html, byte[] BrotliHtml);

public readonly record struct BlogPage(string Title, DateOnly Date, byte[] Html, byte[] BrotliHtml);

internal static class Helpers {
	private static readonly string HeaderHtml = ReadResource("snippets.header.html");
	private static readonly string FooterHtml = ReadResource("snippets.footer.html");
	private static readonly string VNBodyHtml = ReadResource("snippets.vn-body.html");
	private static readonly string HomeCss = ReadResource("snippets.home.css");
	private static readonly string VNCss = ReadResource("snippets.vn.css");
	private static readonly string VNJs = ReadResource("snippets.vn.js");

	public static string ReadResource(string name)
	{
		Assembly asm = Assembly.GetExecutingAssembly();
		using Stream stream = asm.GetManifestResourceStream("Pulp.PublicDomainPulp." + name);
		using StreamReader reader = new StreamReader(stream);
		return reader.ReadToEnd();
	}

	public static IResult HtmlResult(byte[] bytes, int statusCode = 200) {
		return Results.Text(bytes, "text/html; charset=utf-8", statusCode);
	}

	public static void AppendCacheControl(HttpContext context, TimeSpan timespan) {
#if !DEBUG
		context.Response.Headers.CacheControl = $"public, max-age={(int)timespan.TotalSeconds}, immutable";
#endif
	}

	public static byte[] SelectCompressionAndAppendHeaders(HttpContext context, byte[] uncompressed, byte[] compressed) {
		string acceptEncoding = context.Request.Headers.AcceptEncoding.ToString();
		if (acceptEncoding.Contains("br") || acceptEncoding.Contains("*")) {
			context.Response.Headers.ContentEncoding = "br";
			context.Response.Headers.Vary = "Accept-Encoding";
			return compressed;
		}
		return uncompressed;
	}

	public static byte[] BuildContentPage(string html, string title = "Public Domain Pulp") {
		StringBuilder sb = BuildHead(title, [HomeCss], []);
		sb.Append(HeaderHtml);
		sb.Append("<div id='content'>");
		sb.Append(html);
		sb.Append("</div>");
		sb.Append(FooterHtml);
		return GetPageBytes(sb);
	}

	public static byte[] BuildHomePage(Dictionary<string, VisualNovel> visualNovels, Dictionary<string, BlogPage> blogPages) {
		StringBuilder sb = new();
		sb.Append("<style>#nav-home { text-decoration: underline !important; }</style>");

		sb.Append("<h2 class='center'><u>Latest Visual Novels and Blog Posts</u></h2>");

		List<Tuple<DateOnly, string>> posts = new();
		foreach (KeyValuePair<string, BlogPage> kvp in blogPages) {
			string html = $"<div><h3 class='posttitle'><small>{kvp.Value.Date.ToString("MMM dd, yyyy").ToUpper()}</small><br><a href='/blog/{kvp.Key}'>{kvp.Value.Title}</a></h3></div>";
			posts.Add(new Tuple<DateOnly, string>(kvp.Value.Date, html));
		}

		foreach (VisualNovel pulp in visualNovels.Values) {
			StringBuilder html = new();
			html.Append("<div class='pulpcard'>");
			html.Append($"<h3><small>{pulp.Metadata.PulpDate.ToString("MMM dd, yyyy").ToUpper()}</small><br><i>{pulp.Metadata.Title}</i> ({pulp.Metadata.Year}) by {pulp.Metadata.Author}</h3>");
			html.Append("<div><div>");
			html.Append($"<h3><a href='/vn/{pulp.DirName}'>Read <i>{pulp.Metadata.VNTitle}</i></a> ({pulp.Metadata.Words.ToString("N0")} words)</h3>");
			foreach (string line in pulp.Metadata.Blurb.Split('\n')) {
				html.Append($"<p class='indented'>{BookTag.FormatText(line)}</p>");
			}
			html.Append($"<p class='small'>See the <a href='{pulp.Metadata.Repo}'>JohnQPulp/{pulp.DirName} Github repository</a> for offline downloading and issue reporting.</p>");
			html.Append($"<p class='small center'><a href='{pulp.Metadata.Source}'>Epub Source</a>");
			foreach (KeyValuePair<string, string> kvp in pulp.Metadata.Links) {
				html.Append($" • <a href='{kvp.Value}'>{kvp.Key}</a>");
			}
			html.Append("</p>");
			html.Append($"<img src='/vn/{pulp.DirName}/images/c-author.webp'>");
			html.Append($"</div><img src='/vn/{pulp.DirName}/images/preview.webp'>");
			html.Append("</div></div>");
			posts.Add(new Tuple<DateOnly, string>(pulp.Metadata.PulpDate, html.ToString()));
		}

		foreach (Tuple<DateOnly, string> post in posts.OrderBy(p => p.Item1).Reverse()) {
			sb.Append(post.Item2);
		}

		return BuildContentPage(sb.ToString());
	}

	public static byte[] BuildAboutPage() {
		StringBuilder html = new();
		html.Append("<style>#nav-about { text-decoration: underline !important; }\n#content p { max-width: 60em; margin: 20px auto; }</style>");
		html.Append("<p>Public Domain Pulp is a site for creating visual novels out of public domain texts (and perhaps creative commons texts too). ");
		html.Append("The goal is to eventually create visual novels out of most all famous public domain texts.</p>");
		return BuildContentPage(html.ToString(), "About: Public Domain Pulp");
	}

	public static Dictionary<string, BlogPage> BuildBlogPages(string baseDirectory) {
		Dictionary<string, BlogPage> blogPages = new();

		foreach (string file in Directory.GetFiles(Path.Combine(baseDirectory, "CreativeCommonsContent", "blog"), "*.html")) {
			string name = Path.GetFileName(file);
			if (!Regex.IsMatch(name, @"^\d{4}-\d{2}-\d{2} .+\.html$")) throw new Exception("Unexpected blog name.");

			DateOnly date = DateOnly.Parse(name[0..10]);
			string title = name[11..^5];
			bool isProseRoundup = false;
			string formattedTitle = Regex.Replace(title, "Prose Roundup: ([^\\\"\\(]+) ", m => {
				isProseRoundup = true;
				return $"Prose Roundup: <i>{m.Groups[1].Value}</i> ";
			});

			StringBuilder sb = new();
			sb.Append("<div id='blog'>");
			sb.Append($"<h1 id='bloghead' class='center'>{formattedTitle}</h1>");
			sb.Append($"<h3 id='blogsubhead' class='center'><small>{date.ToString("MMM dd, yyyy").ToUpper()}</small></h3>");
			if (isProseRoundup) {
				sb.Append("<p><small>Prose roundups are posts where I run through the noteworthy snippets of books and short stories not yet in the public domain (that therefore can't be made into visual novels yet). The snippets are listed chronologically, grouped by chapters, <b>and may contain spoilers up to their respective locations in their works</b>.</small></p>");
			}
			sb.Append(BookTag.FormatText(File.ReadAllText(file)));
			sb.Append("</div>");
			byte[] bytes = BuildContentPage(sb.ToString(), title);

			blogPages.Add(date.ToString("yyyy-MM-dd"), new(formattedTitle, date, bytes, Compress(bytes)));
		}

		return blogPages;
	}

	public static Dictionary<string, VisualNovel> BuildVisualNovels(string baseDirectory) {
		Dictionary<string, VisualNovel> visualPulps = new(StringComparer.OrdinalIgnoreCase);

		foreach (string dir in Directory.GetDirectories(Path.Combine(baseDirectory, "VisualPulps"))) {
			string name = Path.GetFileName(dir);

			string metadataJson = File.ReadAllText(Path.Combine(dir, "metadata.json"));
			Metadata metadata = Metadata.Parse(metadataJson);

			string rawText = File.ReadAllText(Path.Combine(dir, "book.txt"));
			string pulpText = File.ReadAllText(Path.Combine(dir, "pulp.txt"));

			StringBuilder sb = BuildHead(metadata.VNTitle, [HomeCss, VNCss], [VNJs], $"/vn/{name}/");
			sb.Append(HeaderHtml);
			sb.Append($"<script>window['bookId'] = '{name}';</script>");
			sb.Append("<div id='vn-header'><p><b>To Go Back:</b><br>Click/tap left half of VN<br>OR<br>Left arrow key<br>OR<br>Shift+scroll (up)</p>");
			sb.Append($"<div><h1><i>{metadata.VNTitle}</i></h1><h4>{metadata.Author} • {metadata.Year} • {metadata.Words:N0} Words</h4><h4><a href='{metadata.Repo}'>Github</a> • <a href='{metadata.Source}'>Epub Source</a>");
			foreach (KeyValuePair<string, string> kvp in metadata.Links) {
				sb.Append($" • <a href='{kvp.Value}'>{kvp.Key}</a>");
			}
			sb.Append("</h4></div>");
			sb.Append("<p><b>To Advance:</b><br>Click/tap right half of VN<br>OR<br>Right arrow key<br>OR<br>Shift+scroll (down)</p></div>");
			sb.Append("<main>");
			sb.Append(Helpers.VNBodyHtml);
			sb.Append(Compiler.BuildHtml(rawText, pulpText));
			sb.Append("</main>");
			sb.Append(FooterHtml);
			byte[] bytes = GetPageBytes(sb);

			visualPulps.Add(name, new(name, metadata, bytes, Compress(bytes)));
		}

		return visualPulps;
	}

	private static StringBuilder BuildHead(string title, string[] styles, string[] scripts, string? baseHref = null) {
		StringBuilder sb = new();
		sb.Append("<!DOCTYPE html>");
		sb.Append("<html>");
		sb.Append("<head>");
		sb.Append("<title>" + title + "</title>");
		if (baseHref != null) {
			sb.Append($"<base href='{baseHref}'>");
		}
		sb.Append("<link rel='icon' type='image/x-icon' href='/assets/favicon.ico'>");
		sb.Append("<link rel='icon' type='image/png' href='/assets/icon-144.png' sizes='144x144'>");
		foreach (string style in styles) {
			sb.Append("<style>");
			sb.Append(style);
			sb.Append("</style>");
		}
		foreach (string script in scripts) {
			sb.Append("<script>");
			sb.Append(script);
			sb.Append("</script>");
		}
		sb.Append("</head>");
		sb.Append("<body>");
		return sb;
	}

	private static byte[] GetPageBytes(StringBuilder sb) {
		sb.Append("</body></html>");
		return Encoding.UTF8.GetBytes(sb.ToString());
	}

	private static byte[] Compress(byte[] input)
	{
		using MemoryStream output = new();
		using (BrotliStream brotli = new(output, CompressionLevel.SmallestSize, leaveOpen: true))
		{
			brotli.Write(input, 0, input.Length);
		}
		return output.ToArray();
	}
}