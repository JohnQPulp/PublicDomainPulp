using System.Reflection;
using System.Text;
using Pulp.Pulpifier;

namespace Pulp.PublicDomainPulp;

public readonly record struct VisualNovel(string DirName, Metadata Metadata, string Html);

internal static class Helpers {
	public static readonly string HeadHtml = ReadResource("snippets.head.html");
	public static readonly string VNBodyHtml = ReadResource("snippets.vn-body.html");
	public static readonly string HomeBodyHtml = ReadResource("snippets.home-body.html");
	
	public static string ReadResource(string name)
	{
		Assembly asm = Assembly.GetExecutingAssembly();
		using Stream stream = asm.GetManifestResourceStream("Pulp.PublicDomainPulp." + name);
		using StreamReader reader = new StreamReader(stream);
		return reader.ReadToEnd();
	}

	public static string BuildContentPage(string html) {
		return (HeadHtml + HomeBodyHtml).Replace("<div id='content'></div>", $"<div id='content'>{html}</div>");
	}

	public static string BuildHomePage(Dictionary<string, VisualNovel> visualNovels) {
		StringBuilder html = new();
		html.Append("<style>#nav-home { text-decoration: underline !important; }</style>");
		foreach (VisualNovel pulp in visualNovels.Values) {
			html.Append("<div class='pulpcard'>");
			html.Append($"<h3><i>{pulp.Metadata.Title}</i> ({pulp.Metadata.Year}) by {pulp.Metadata.Author}</h3>");
			html.Append("<div><div>");
			html.Append($"<h3><a href='/vn/{pulp.DirName}/pulp.html'>Read Online</a></h3>");
			html.Append($"<p>{pulp.Metadata.Blurb}</p>");
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

	public static string BuildAboutPage() {
		StringBuilder html = new();
		html.Append("<style>#nav-about { text-decoration: underline !important; }</style>");
		html.Append("<p>Public Domain Pulp is a site for creating visual novels out of public domain texts (and perhaps creative commons texts too).");
		html.Append("The goal is to eventually create visual novels out of most all famous public domain texts.</p>");
		return BuildContentPage(html.ToString());
	}

	public static Dictionary<string, VisualNovel> BuildVisualNovels(string baseDirectory) {
		Dictionary<string, VisualNovel> visualPulps = new(StringComparer.OrdinalIgnoreCase);

		foreach (string dir in Directory.GetDirectories(Path.Combine(baseDirectory, "VisualPulps"))) {
			string name = Path.GetFileName(dir);

			string metadataJson = File.ReadAllText(Path.Combine(dir, "metadata.json"));
			Metadata metadata = Metadata.Parse(metadataJson);
	
			string rawText = File.ReadAllText(Path.Combine(dir, "book.txt"));
			string pulpText = File.ReadAllText(Path.Combine(dir, "pulp.txt"));

			string html = Helpers.HeadHtml + Helpers.VNBodyHtml + Compiler.BuildHtml(rawText, pulpText);

			visualPulps.Add(name, new(name, metadata, html));
		}
		
		return visualPulps;
	}
}