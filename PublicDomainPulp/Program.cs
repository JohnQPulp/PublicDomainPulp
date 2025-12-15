using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.FileProviders;
using Microsoft.Net.Http.Headers;
using Pulp.PublicDomainPulp;
using Pulp.Pulpifier;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
WebApplication app = builder.Build();

string baseDirectory = Directory.GetCurrentDirectory().Substring(0, Directory.GetCurrentDirectory().IndexOf("/PublicDomainPulp/", StringComparison.Ordinal)) + "/PublicDomainPulp";
Dictionary<string, VisualNovel> visualPulps = Helpers.BuildVisualNovels(baseDirectory);

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

string homeHtml = Helpers.BuildHomePage(visualPulps);
app.MapGet("/", () => {
	return Results.Text(homeHtml, "text/html; charset=utf-8", Encoding.UTF8, 200);
});

string aboutHtml = Helpers.BuildAboutPage();
app.MapGet("/about", () => {
	return Results.Text(aboutHtml, "text/html; charset=utf-8", Encoding.UTF8, 200);
});

app.MapGet("/vn/{book}/pulp.html", (string book) =>
{
	if (!visualPulps.TryGetValue(book, out VisualNovel pulp)) {
		return Results.NotFound();
	}
	return Results.Text(pulp.Html, "text/html; charset=utf-8", Encoding.UTF8, 200);
});

app.MapGet("/vn/{book}/images/{image}.webp", async (string book, string image, HttpContext context) =>
{
	if (visualPulps.TryGetValue(book, out VisualNovel pulp)) {
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