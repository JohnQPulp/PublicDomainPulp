using System.Net;
using System.Net.Mime;
using System.Text;
using Pulp.Pulpifier;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
WebApplication app = builder.Build();

Dictionary<string, VisualPulp> visualPulps = new(StringComparer.OrdinalIgnoreCase);

foreach (string dir in Directory.GetDirectories("../VisualPulps")) {
	string name = Path.GetFileName(dir);

	string rawText = File.ReadAllText(Path.Combine(dir, "book.txt"));
	string pulpText = File.ReadAllText(Path.Combine(dir, "pulp.txt"));

	string html = Compiler.BuildHtml(rawText, pulpText);

	visualPulps.Add(name, new(name, html));
}

app.MapGet("/", () => "Hello World!");

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

	string path = Path.GetFullPath(Path.Combine("..", "VisualPulps", pulp.DirName, "images", image + ".webp"));

	return Results.File(path, "image/webp");
});

app.Run();


public readonly record struct VisualPulp(string DirName, string Html);