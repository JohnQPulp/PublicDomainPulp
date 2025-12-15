using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Net.Http.Headers;
using Pulp.PublicDomainPulp;
using Pulp.Pulpifier;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options =>
{
	options.AddServerHeader = false;
});
WebApplication app = builder.Build();

string baseDirectory = Directory.GetCurrentDirectory().Substring(0, Directory.GetCurrentDirectory().IndexOf("/PublicDomainPulp/", StringComparison.Ordinal)) + "/PublicDomainPulp";
Dictionary<string, VisualNovel> visualPulps = Helpers.BuildVisualNovels(baseDirectory);

app.Use(async (context, next) =>
{
	context.Response.Headers[HeaderNames.CacheControl] = "no-store";
	context.Response.Headers[HeaderNames.XContentTypeOptions] = "nosniff";

	await next.Invoke();

	Debug.Assert(context.Response.Headers[HeaderNames.XContentTypeOptions] == "nosniff");

	Debug.Assert(context.Response.Headers.ContainsKey(HeaderNames.CacheControl));
	Debug.Assert(!context.Response.Headers.ContainsKey(HeaderNames.ETag));
	Debug.Assert(!context.Response.Headers.ContainsKey(HeaderNames.LastModified));
	Debug.Assert(!context.Response.Headers.ContainsKey(HeaderNames.Expires));
	Debug.Assert(!context.Response.Headers.ContainsKey(HeaderNames.Pragma));

	Debug.Assert(!context.Response.Headers.ContainsKey(HeaderNames.Server));
	Debug.Assert(!context.Response.Headers.ContainsKey(HeaderNames.XPoweredBy));

	Debug.Assert(context.Response.ContentLength == 0 || context.Response.Headers.ContainsKey(HeaderNames.ContentType));
});

app.UseStaticFiles(new StaticFileOptions
{
	FileProvider = new PhysicalFileProvider(Path.Combine(baseDirectory, "PublicDomainPulp", "assets")),
	RequestPath = "/assets",
	ContentTypeProvider = new FileExtensionContentTypeProvider {
		Mappings = {
			[".css"] = "text/css; charset=utf-8",
			[".js"] = "application/javascript; charset=utf-8"
		}
	},
	OnPrepareResponse = ctx => {
		HttpResponse res = ctx.Context.Response;
		res.Headers.Remove(HeaderNames.ETag);
		res.Headers.Remove(HeaderNames.LastModified);
		res.Headers.CacheControl = "public, max-age=3600, immutable";
	}
});

string homeHtml = Helpers.BuildHomePage(visualPulps);
app.MapGet("/", () => {
	return Helpers.HtmlResult(homeHtml);
});

string aboutHtml = Helpers.BuildAboutPage();
app.MapGet("/about", () => {
	return Helpers.HtmlResult(aboutHtml);
});

string notFoundHtml = Helpers.BuildContentPage("<h2>404 Not Found</h2>");
app.MapGet("/vn/{book:regex(^[A-Za-z]{{1,100}}$)}/pulp.html", (string book) =>
{
	if (!visualPulps.TryGetValue(book, out VisualNovel pulp)) {
		return Helpers.HtmlResult(notFoundHtml, 404);
	}
	return Helpers.HtmlResult(pulp.Html);
});

app.MapGet("/vn/{book:regex(^[A-Za-z]{{1,100}}$)}/images/{image:regex(^[a-z0-9-]{{1,100}}$)}.webp", async (string book, string image, HttpContext context) =>
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

app.MapFallback(() =>
{
	return Helpers.HtmlResult(notFoundHtml, 404);
});

app.Run();