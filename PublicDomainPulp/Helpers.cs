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

	private static string ReadResource(string name)
	{
		Assembly asm = Assembly.GetExecutingAssembly();
		using Stream stream = asm.GetManifestResourceStream("Pulp.PublicDomainPulp." + name);
		using StreamReader reader = new StreamReader(stream);
		return reader.ReadToEnd();
	}

	public static void MapPage(WebApplication app, string route, byte[] page, TimeSpan cacheTime) {
		byte[] compressedPage = Compress(page);
		app.MapGet(route, (HttpContext context) => {
			AppendCacheControl(context, cacheTime);
			if (context.Request.Path.ToString() != route) return Results.Redirect(route, true);
			byte[] html = SelectCompressionAndAppendHeaders(context, page, compressedPage);
			return HtmlResult(html);
		});
	}

	public static IResult HtmlResult(byte[] bytes, int statusCode = 200) {
		return Results.Text(bytes, "text/html; charset=utf-8", statusCode);
	}

	public static void AppendCacheControl(HttpContext context, TimeSpan timespan) {
#if DEBUG
		string path = context.Request.Path.ToString();
		if (!(path.EndsWith(".webp") || path.EndsWith(".avif"))) return;
#endif

		context.Response.Headers.CacheControl = $"public, max-age={(int)timespan.TotalSeconds}, immutable";
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

	public static byte[] BuildContentPage(string html, string? title) {
		StringBuilder sb = BuildHead(title, [HomeCss], []);
		sb.Append(HeaderHtml);
		sb.Append("<div id='content'>");
		sb.Append(html);
		sb.Append("</div>");
		sb.Append(FooterHtml);
		return GetPageBytes(sb);
	}

	internal static byte[] BuildBlogPage(string html, string title) {
		StringBuilder sb = BuildHead(title, [HomeCss], []);
		sb.Append(HeaderHtml);
		sb.Append("<div id='content' class='blogwrapper'>");
		sb.Append("<div id='b-left' class='svisible ed'></div>");
		sb.Append("<div id='b-left' class='svisible ned'></div>");
		sb.Append("<div id='blog'>");
		sb.Append(html);
		sb.Append("</div>");
		sb.Append("<div id='b-right' class='svisible ed'></div>");
		sb.Append("<div id='b-right' class='svisible ned'></div>");
		sb.Append("</div>");
		sb.Append(FooterHtml);
		return GetPageBytes(sb);
	}

	public static byte[] BuildHomePage(Dictionary<string, VisualNovel> visualNovels) {
		StringBuilder sb = new();
		sb.Append("<p id='myblurb' class='center'>Public Domain Pulp’s visual novel adaptations are all free, unabridged, and dedicated to the public&nbsp;domain.</p>");
		sb.Append("<h2 id='newest' class='center'>Newest Releases:</h2>");
		if (visualNovels.Count > 1) {
			sb.Append("<div id='homecardwrapper'>");
			List<VisualNovel> vns = visualNovels.Values.OrderByDescending(vn => vn.Metadata.PulpDate).Take(2).ToList();
			foreach (VisualNovel vn in vns) {
				sb.Append($"<a id='homecard' href='/vn/{vn.DirName}'>");
				sb.Append($"<h3 class='center'><i>{vn.Metadata.ShortTitle}: The Visual&nbsp;Novel</i></h3>");
				string imageExtension = vn.Metadata.ImageExtension;
				sb.Append($"<img class='opp' src='/vn/{vn.DirName}/images/preview-small.{imageExtension}' loading='lazy'>");
				sb.Append($"<img class='nopp' src='/vn/{vn.DirName}/images/preview.{imageExtension}' loading='lazy'>");
				sb.Append($"<h4 class='center'>{vn.Metadata.Author} • {vn.Metadata.Year} • {vn.Metadata.Words.ToString("N0")}&nbsp;words</h4>");
				sb.Append("</a>");
			}
			sb.Append("</div>");
		}

		sb.Append("<h1 class='center'><a href='/catalog'>Browse the Full Catalog</a></h1>");

		return BuildContentPage(sb.ToString(), null);
	}

	public static byte[] BuildAboutPage(string baseDirectory) {
		string path = Path.Combine(baseDirectory, "CreativeCommonsContent", "about.html");
		string html = BookTag.FormatText(File.ReadAllText(path));
		return BuildBlogPage(html, "About Public Domain Pulp");
	}

	public static byte[] BuildCatalogPage(Dictionary<string, VisualNovel> visualNovels) {
		StringBuilder html = new();
		html.Append("<h1 class='center'>Visual Novel Catalog</h1>");
		List<VisualNovel> visualNovelList = visualNovels.Values.OrderByDescending(vn => vn.Metadata.PulpDate).ToList();
		foreach (VisualNovel pulp in visualNovelList) {
			html.Append("<div class='pulpcard'>");
			html.Append($"<h3><small class='upper'>{pulp.Metadata.PulpDate.Value.ToString("MMM dd, yyyy")}</small><br><i>{pulp.Metadata.Title}</i> ({pulp.Metadata.Year}) by {pulp.Metadata.Author}</h3>");
			string imageExtension = pulp.Metadata.ImageExtension;
			html.Append($"<img class='op' src='/vn/{pulp.DirName}/images/preview-small.{imageExtension}' loading='lazy'>");
			html.Append("<div><div>");
			html.Append($"<h3><a href='/vn/{pulp.DirName}'>Read <i>{pulp.Metadata.ShortTitle}: The Visual&nbsp;Novel</i></a> ({pulp.Metadata.Words.ToString("N0")}&nbsp;words)</h3>");
			foreach (string line in pulp.Metadata.Blurb.Split('\n')) {
				html.Append($"<p class='blurb ned'>{BookTag.FormatText(line)}</p>");
			}
			if (pulp.Metadata.Blurb2 != null) {
				foreach (string line in pulp.Metadata.Blurb2.Split('\n')) {
					html.Append($"<p class='blurb ed'>{BookTag.FormatText(line)}</p>");
				}
			}
			html.Append($"<p class='small'>See the <a href='{pulp.Metadata.Repo}'>JohnQPulp/{pulp.DirName} Github repository</a> for offline downloading and issue reporting.</p>");
			html.Append($"<p class='small center'><a href='{pulp.Metadata.Source}'>Standard Ebooks</a>");
			foreach (KeyValuePair<string, string> kvp in pulp.Metadata.Links) {
				html.Append($" • <a href='{kvp.Value}'>{kvp.Key}</a>");
			}
			html.Append("</p>");
			html.Append($"<img class='ned' src='/vn/{pulp.DirName}/images/c-author.{imageExtension}' loading='lazy'>");
			html.Append($"<img class='ed' src='/vn/{pulp.DirName}/images/c-author-abased.{imageExtension}' loading='lazy'>");
			html.Append($"</div><img class='nop' src='/vn/{pulp.DirName}/images/preview.{imageExtension}' loading='lazy'>");
			html.Append("</div></div>");
		}
		return BuildContentPage(html.ToString(), "Visual Novel Catalog");
	}

	public static byte[] BuildUpcomingsPage(List<Metadata> upcomings) {
		StringBuilder sb = new();
		sb.Append("<style>");
		sb.Append("table {margin: 0 auto;}");
		sb.Append("td {padding-bottom: 10px;}");
		sb.Append("th {font-size: 1.25em;}");
		sb.Append(".tc {text-align: center;}");
		sb.Append(".tr {text-align: right;}");
		sb.Append("@media (width > 1000px) { table {font-size: 1.2em;}}");
		sb.Append("</style>");
		sb.Append("<h1 class='center'>Upcoming Visual Novels</h1><table><tr><th>Title</th><th>Author</th><th>Words</th></tr>");
		upcomings.Sort((a, b) => a.Title.CompareTo(b.Title, StringComparison.Ordinal));
		foreach (Metadata upcoming in upcomings) {
			sb.Append("<tr>");
			sb.Append($"<td><i>{upcoming.Title}</i></td>");
			sb.Append($"<td class='tc'>{upcoming.Author}</td>");
			sb.Append($"<td class='tr'>{upcoming.Words:N0}</td>");
			sb.Append("</tr>");
		}
		sb.Append("</table>");
		return BuildContentPage(sb.ToString(), "Upcoming Visual Novels");
	}

	public static byte[] BuildBlogsPage(Dictionary<string, BlogPage> blogPages) {
		StringBuilder sb = new();
		sb.Append("<h1 class='center'>Blog Posts</h1>");
		List<BlogPage> blogs = blogPages.Values.ToList();
		blogs.Sort((a, b) => b.Date.CompareTo(a.Date));
		foreach (BlogPage blog in blogs) {
			sb.Append($"<div><h3 class='posttitle'><small class='upper'>{blog.Date.ToString("MMM dd, yyyy")}</small><br><a href='/blog/{blog.Date:yyyy-MM-dd}'>{blog.Title}</a></h3></div>");
		}
		return BuildContentPage(sb.ToString(), "Blog Posts");
	}

	public static byte[] BuildContactPage(string baseDirectory) {
		string path = Path.Combine(baseDirectory, "CreativeCommonsContent", "contact.html");
		string html = BookTag.FormatText(File.ReadAllText(path));
		return BuildBlogPage(html, "Contacting John Q. Pulp");
	}

	public static Dictionary<string, BlogPage> BuildBlogPages(string baseDirectory) {
		Dictionary<string, BlogPage> blogPages = new();

		foreach (string file in Directory.GetFiles(Path.Combine(baseDirectory, "CreativeCommonsContent", "blog"), "*.html")) {
			string name = Path.GetFileName(file);
			Match match = Regex.Match(name, @"^((\d{4}-\d{2}-\d{2}) )?(.+)\.html$");
			if (!match.Success) throw new Exception("Unexpected blog name.");

			if (match.Groups[2].Value == "") continue;

			string title = match.Groups[3].Value;
			DateOnly date = DateOnly.Parse(match.Groups[2].Value);

			StringBuilder sb = new();
			sb.Append($"<h1 id='bloghead' class='center'>{title}</h1>");
			sb.Append($"<h3 id='blogsubhead' class='center'><small class='upper'>{date.ToString("MMM dd, yyyy")}</small></h3>");
			sb.Append(BookTag.FormatText(File.ReadAllText(file)));
			byte[] bytes = BuildBlogPage(sb.ToString(), title);

			blogPages.Add(date.ToString("yyyy-MM-dd"), new(title, date, bytes, Compress(bytes)));
		}

		return blogPages;
	}

	public static (Dictionary<string, VisualNovel>, List<Metadata>) BuildVisualNovels(string baseDirectory) {
		Dictionary<string, VisualNovel> visualPulps = new(StringComparer.OrdinalIgnoreCase);
		List<Metadata> upcomings = new List<Metadata>();

		foreach (string dir in Directory.GetDirectories(Path.Combine(baseDirectory, "VisualPulps"))) {
			string name = Path.GetFileName(dir);

			string metadataJson = File.ReadAllText(Path.Combine(dir, "metadata.json"));
			Metadata metadata = Metadata.Parse(metadataJson);

			if (metadata.PulpDate == null) {
				upcomings.Add(metadata);
				continue;
			}

			byte[] bytes = BuildVisualNovel(name, dir, metadata);

			visualPulps.Add(name, new(name, metadata, bytes, Compress(bytes)));
		}

		return (visualPulps, upcomings);
	}

	internal static byte[] BuildVisualNovel(string name, string dir, Metadata metadata) {
		string rawText = File.ReadAllText(Path.Combine(dir, "book.txt"));
		string pulpText = File.ReadAllText(Path.Combine(dir, "pulp.txt"));

		StringBuilder sb = BuildHead(metadata.VNTitle, [HomeCss, VNCss], [VNJs], $"/vn/{name}/");
		sb.Append(HeaderHtml);
		sb.Append($"<script>window['bookId'] = '{name}';</script>");
		sb.Append("<div id='vn-header' class='smargin'><p><b>To Go Back:</b><br>Click/tap left half of VN<br><span class='nopp'>OR<br>Left arrow key<br>OR<br>Shift+scroll (up)</span></p>");
		sb.Append($"<div id='vn-header-mid'><h1><i>{metadata.ShortTitle}: The Visual&nbsp;Novel</i></h1><h4>{metadata.Author} • {metadata.Year} • {metadata.Words:N0} Words</h4><h4><a href='{metadata.Repo}'>Github</a>");
		foreach (KeyValuePair<string, string> kvp in metadata.Links) {
			sb.Append($" • <a href='{kvp.Value}'>{kvp.Key}</a>");
		}
		sb.Append($"</h4><small>(This visual novel's text is unmodified<a href='/blog/2026-02-13#text-lines'>*</a> from its original <a href='{metadata.Source}'>Standard Ebooks .epub source</a>.)</small></div>");
		sb.Append("<p><b>To Advance:</b><br>Click/tap right half of VN<br><span class='nopp'>OR<br>Right arrow key<br>OR<br>Shift+scroll (down)</span></p></div>");
		sb.Append("<main>");
		sb.Append(VNBodyHtml);
		sb.Append("<div id='appwrapper'>");
		sb.Append(Compiler.BuildHtml(rawText, pulpText, out Dictionary<string, ImageMetadata> files, metadata.ImageExtension));
		sb.Append("</div>");
#if DEBUG
		if (metadata.PulpDate != null) {
			foreach (KeyValuePair<string, ImageMetadata> kvp in files) {
				string file = $"{kvp.Key}.{metadata.ImageExtension}";
				if (!File.Exists(Path.Combine(dir, "images", file))) throw new Exception($"Missing image: {file}");
			}
		}
#endif
		sb.Append("</main>");
		sb.Append(FooterHtml);

		return GetPageBytes(sb);
	}

	private static StringBuilder BuildHead(string? title, string[] styles, string[] scripts, string? baseHref = null) {
		StringBuilder sb = new();
		sb.Append("<!DOCTYPE html>");
		sb.Append("<html>");
		sb.Append("<head>");
		string fullTitle = title == null ? "Public Domain Pulp: Classic Literature as Visual Novels, Free and Unabridged" : (title + " | Public Domain Pulp");
		sb.Append("<title>" + fullTitle + "</title>");
		if (baseHref != null) {
			sb.Append($"<base href='{baseHref}'>");
		}
		sb.Append("<link rel='icon' type='image/x-icon' href='/assets/favicon.ico'>");
		sb.Append("<link rel='icon' type='image/png' href='/assets/icon-192.png' sizes='192x192'>");
		sb.Append("<meta name='viewport' content='width=device-width, initial-scale=1.0'>");
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