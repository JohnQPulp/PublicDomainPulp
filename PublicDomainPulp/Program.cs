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
	
	string html = "PublicDomainPulp is a website for hosting visual novel transformation (pulpifications) of public domain (and creative commons) fiction books. The current catalog:<br><ul>";
	html += string.Join("<br>", visualPulps.Values.Select(vp => $"<li><a href='/vn/{vp.DirName}/pulp.html'>{vp.Metadata.Title}</a> by {vp.Metadata.Author}<img src='/vn/{vp.DirName}/images/preview.webp' /></li>"));
	html += "</ul>";

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