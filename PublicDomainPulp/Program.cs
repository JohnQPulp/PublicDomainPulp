using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.FileProviders;
using Microsoft.Net.Http.Headers;
using Pulp.PublicDomainPulp;
using Pulp.Pulpifier;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
WebApplication app = builder.Build();

Dictionary<string, VisualPulp> visualPulps = new(StringComparer.OrdinalIgnoreCase);

string baseDirectory = Directory.GetCurrentDirectory().Substring(0, Directory.GetCurrentDirectory().IndexOf("/PublicDomainPulp/", StringComparison.Ordinal)) + "/PublicDomainPulp";

foreach (string dir in Directory.GetDirectories(Path.Combine(baseDirectory, "VisualPulps"))) {
	string name = Path.GetFileName(dir);

	string metadataJson = File.ReadAllText(Path.Combine(dir, "metadata.json"));
	Metadata metadata = Metadata.Parse(metadataJson);
	
	string rawText = File.ReadAllText(Path.Combine(dir, "book.txt"));
	string pulpText = File.ReadAllText(Path.Combine(dir, "pulp.txt"));

	string html = Helpers.HeadHtml + Helpers.VNBodyHtml + Compiler.BuildHtml(rawText, pulpText);

	visualPulps.Add(name, new(name, metadata, html));
}

app.Use((context, next) =>
{
	context.Response.Headers[HeaderNames.XContentTypeOptions] = "nosniff";
	return next();
});

app.UseStaticFiles(new StaticFileOptions
{
	FileProvider = new PhysicalFileProvider(Path.Combine(baseDirectory, "PublicDomainPulp", "assets")),
	RequestPath = "/assets"
});

app.MapGet("/", () => {
	string homeHtml = Helpers.HeadHtml + Helpers.HomeBodyHtml;

	StringBuilder html = new();
	foreach (VisualPulp pulp in visualPulps.Values) {
		html.Append("<div class='pulpcard'>");
		html.Append($"<h3><i>{pulp.Metadata.Title}</i> ({pulp.Metadata.Year}) by {pulp.Metadata.Author}</h3>");
		html.Append("<div><div>");
		html.Append($"<h3><a href='/vn/{pulp.DirName}/pulp.html'>Read Online</a></h3>");
		html.Append($"<p>{pulp.Metadata.Blurb}</p>");
		html.Append($"<p>Github link: <a href='{pulp.Metadata.Source}'>{pulp.Metadata.Source}</a></p>");
		html.Append("<p>External Links:</p><ul>");
		foreach (string link in pulp.Metadata.Links) {
			html.Append($"<li><a href='{link}'>{link}</a></li>");
		}
		html.Append("</ul>");
		html.Append($"<img src='/vn/{pulp.DirName}/images/c-author.webp'>");
		html.Append($"</div><img src='/vn/{pulp.DirName}/images/preview.webp'>");
		html.Append("</div></div>");
	}

	string finalHtml = homeHtml.Replace("<div id='content'></div>", $"<div id='content'>{html}</div>");
	
	return Results.Text(finalHtml, "text/html; charset=utf-8", Encoding.UTF8, 200);
});

app.MapGet("/vn/{book}/pulp.html", (string book) =>
{
	if (!visualPulps.TryGetValue(book, out VisualPulp pulp)) {
		return Results.NotFound();
	}
	return Results.Text(pulp.Html, "text/html; charset=utf-8", Encoding.UTF8, 200);
});

app.MapGet("/vn/{book}/images/{image}.webp", async (string book, string image, HttpContext context) =>
{
	if (visualPulps.TryGetValue(book, out VisualPulp pulp)) {
		context.Response.ContentType = "image/webp";
		string path = Path.Combine(baseDirectory, "VisualPulps", pulp.DirName, "images", image + ".webp");

		try {
			context.Response.StatusCode = 200;
			context.Response.Headers.CacheControl = "public, max-age=604800, immutable";
			await context.Response.SendFileAsync(path);
			return Results.Empty;
		} catch (FileNotFoundException) { }
	}

	context.Response.StatusCode = 404;
	return Results.Empty;
});

app.Run();

public readonly record struct VisualPulp(string DirName, Metadata Metadata, string Html);