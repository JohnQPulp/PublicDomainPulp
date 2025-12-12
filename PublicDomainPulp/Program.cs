using System.Text;
using Microsoft.Extensions.FileProviders;
using Microsoft.Net.Http.Headers;
using Pulp.Pulpifier;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
WebApplication app = builder.Build();

Dictionary<string, VisualPulp> visualPulps = new(StringComparer.OrdinalIgnoreCase);

string baseDirectory = Directory.GetCurrentDirectory().Substring(0, Directory.GetCurrentDirectory().IndexOf("/PublicDomainPulp/", StringComparison.Ordinal)) + "/PublicDomainPulp";

foreach (string dir in Directory.GetDirectories(Path.Combine(baseDirectory, "VisualPulps"))) {
	string name = Path.GetFileName(dir);

	string rawText = File.ReadAllText(Path.Combine(dir, "book.txt"));
	string pulpText = File.ReadAllText(Path.Combine(dir, "pulp.txt"));

	string html = Compiler.BuildHtml(rawText, pulpText);

	visualPulps.Add(name, new(name, html));
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
	string html = "<head><title>Public Domain Pulp</title><link rel='icon' type='image/x-icon' href='/assets/favicon.ico'><link href='/assets/style.css' rel='stylesheet'></head><body>";
	html += "PublicDomainPulp is a website for hosting visual novel transformation (pulpifications) of public domain (and creative commons) fiction books. The current catalog:<br><ul>";
	html += string.Join("<br>", visualPulps.Values.Select(vp => $"<li><a href='/vn/{vp.DirName}/'>{vp.DirName}</a></li>"));
	html += "</ul>";
	return Results.Text(html, "text/html; charset=utf-8", Encoding.UTF8, 200);
});

app.MapGet("/vn/{book}/", (string book) =>
{
	if (!visualPulps.TryGetValue(book, out VisualPulp pulp)) {
		return Results.NotFound();
	}
	return Results.Text(pulp.Html, "text/html; charset=utf-8", Encoding.UTF8, 200);
});

app.MapGet("/vn/{book}/images/{image}.webp", (string book, string image) =>
{
	if (!visualPulps.TryGetValue(book, out VisualPulp pulp)) {
		return Results.NotFound();
	}

	string path = Path.Combine(baseDirectory, "VisualPulps", pulp.DirName, "images", image + ".webp");

	return Results.File(path, "image/webp");
});

app.Run();

public readonly record struct VisualPulp(string DirName, string Html);