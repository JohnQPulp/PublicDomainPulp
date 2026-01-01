using System.IO.Compression;
using System.Reflection;
using System.Text;
using Pulp.Pulpifier;

namespace Pulp.PublicDomainPulp;

public readonly record struct VisualNovel(string DirName, Metadata Metadata, byte[] Html, byte[] BrotliHtml);

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

	public static byte[] BuildContentPage(string html, string title = "Public Domain Pulp") {
		StringBuilder sb = BuildHead(title, [HomeCss], []);
		sb.Append(HeaderHtml);
		sb.Append("<div id='content'>");
		sb.Append(html);
		sb.Append("</div>");
		sb.Append(FooterHtml);
		return GetPageBytes(sb);
	}

	public static byte[] BuildHomePage(Dictionary<string, VisualNovel> visualNovels) {
		StringBuilder html = new();
		html.Append("<style>#nav-home { text-decoration: underline !important; }</style>");
		foreach (VisualNovel pulp in visualNovels.Values) {
			html.Append("<div class='pulpcard'>");
			html.Append($"<h3><i>{pulp.Metadata.Title}</i> ({pulp.Metadata.Year}) by {pulp.Metadata.Author}</h3>");
			html.Append("<div><div>");
			html.Append($"<h3><a href='/vn/{pulp.DirName}/pulp.html'>Read Online</a> ({pulp.Metadata.Words.ToString("N0")} words)</h3>");
			foreach (string line in pulp.Metadata.Blurb.Split('\n')) {
				html.Append($"<p class='indented'>{line}</p>");
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
		}
		return BuildContentPage(html.ToString());
	}

	public static byte[] BuildAboutPage() {
		StringBuilder html = new();
		html.Append("<style>#nav-about { text-decoration: underline !important; }\n#content p { max-width: 60em; margin: 20px auto; }</style>");
		html.Append("<p>Public Domain Pulp is a site for creating visual novels out of public domain texts (and perhaps creative commons texts too). ");
		html.Append("The goal is to eventually create visual novels out of most all famous public domain texts.</p>");
		return BuildContentPage(html.ToString(), "About: Public Domain Pulp");
	}

	public static Dictionary<string, VisualNovel> BuildVisualNovels(string baseDirectory) {
		Dictionary<string, VisualNovel> visualPulps = new(StringComparer.OrdinalIgnoreCase);

		foreach (string dir in Directory.GetDirectories(Path.Combine(baseDirectory, "VisualPulps"))) {
			string name = Path.GetFileName(dir);

			string metadataJson = File.ReadAllText(Path.Combine(dir, "metadata.json"));
			Metadata metadata = Metadata.Parse(metadataJson);

			string rawText = File.ReadAllText(Path.Combine(dir, "book.txt"));
			string pulpText = File.ReadAllText(Path.Combine(dir, "pulp.txt"));

			string title = metadata.ShortTitle + ": The Visual Novel";
			StringBuilder sb = BuildHead(title, [HomeCss, VNCss], [VNJs]);
			sb.Append(HeaderHtml);
			sb.Append($"<script>window['bookId'] = '{name}';</script>");
			sb.Append($"<div id='vn-header'><p><b>To Go Back:</b><br>Click/tap left half of VN<br>OR<br>Left arrow key<br>OR<br>Shift+scroll (up)</p><h1>{title}</h1><p><b>To Advance:</b><br>Click/tap right half of VN<br>OR<br>Right arrow key<br>OR<br>Shift+scroll (down)</p></div>");
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

	private static StringBuilder BuildHead(string title, string[] styles, string[] scripts) {
		StringBuilder sb = new();
		sb.Append("<!DOCTYPE html>");
		sb.Append("<html>");
		sb.Append("<head>");
		sb.Append("<title>" + title + "</title>");
		sb.Append("<link rel='icon' type='image/x-icon' href='/assets/favicon.ico'>");
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