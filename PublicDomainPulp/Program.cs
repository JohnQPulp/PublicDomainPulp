using System.Diagnostics;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Net.Http.Headers;
using Pulp.PublicDomainPulp;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options =>
{
	options.AddServerHeader = false;
});
WebApplication app = builder.Build();

string baseDirectory = Directory.GetCurrentDirectory().Substring(0, Directory.GetCurrentDirectory().IndexOf("/PublicDomainPulp/", StringComparison.Ordinal)) + "/PublicDomainPulp";
Dictionary<string, VisualNovel> visualPulps = Helpers.BuildVisualNovels(baseDirectory);
Dictionary<string, BlogPage> blogPages = Helpers.BuildBlogPages(baseDirectory);

app.Use(async (HttpContext context, RequestDelegate next) =>
{
	context.Response.Headers[HeaderNames.CacheControl] = "no-store";
	context.Response.Headers[HeaderNames.XContentTypeOptions] = "nosniff";

	await next(context);

	Debug.Assert(context.Response.Headers[HeaderNames.XContentTypeOptions] == "nosniff");

	Debug.Assert(context.Response.Headers.ContainsKey(HeaderNames.CacheControl));
	Debug.Assert(!context.Response.Headers.ContainsKey(HeaderNames.ETag));
	Debug.Assert(!context.Response.Headers.ContainsKey(HeaderNames.LastModified));
	Debug.Assert(!context.Response.Headers.ContainsKey(HeaderNames.Expires));
	Debug.Assert(!context.Response.Headers.ContainsKey(HeaderNames.Pragma));

	Debug.Assert(!context.Response.Headers.ContainsKey(HeaderNames.Server));
	Debug.Assert(!context.Response.Headers.ContainsKey(HeaderNames.XPoweredBy));

	Debug.Assert(context.Response.StatusCode != 200 || context.Response.ContentLength == 0 || context.Response.Headers.ContainsKey(HeaderNames.ContentType));

	Debug.Assert(!context.Response.Headers.ContainsKey(HeaderNames.ContentEncoding) || context.Response.Headers.Vary == "Accept-Encoding");
});

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
	ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
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
		Helpers.AppendCacheControl(ctx.Context, TimeSpan.FromDays(1));
	}
});

app.UseStaticFiles(new StaticFileOptions
{
	FileProvider = new PhysicalFileProvider(Path.Combine(baseDirectory, "CreativeCommonsContent", "images")),
	RequestPath = "/images",
	OnPrepareResponse = ctx => {
		HttpResponse res = ctx.Context.Response;
		res.Headers.Remove(HeaderNames.ETag);
		res.Headers.Remove(HeaderNames.LastModified);
		Helpers.AppendCacheControl(ctx.Context, TimeSpan.FromDays(30));
	}
});

byte[] homeHtml = Helpers.BuildHomePage(visualPulps, blogPages);
app.MapGet("/", (HttpContext context) => {
	Helpers.AppendCacheControl(context, TimeSpan.FromDays(1));
	return Helpers.HtmlResult(homeHtml);
});

byte[] aboutHtml = Helpers.BuildAboutPage();
app.MapGet("/about", (HttpContext context) => {
	Helpers.AppendCacheControl(context, TimeSpan.FromDays(1));
	return Helpers.HtmlResult(aboutHtml);
});

byte[] notFoundHtml = Helpers.BuildContentPage("<h2>404 Not Found</h2>");
app.MapGet("/blog/{date:regex(\\d{{4}}-\\d{{2}}-\\d{{2}})}", (string date, HttpContext context) => {
	if (!blogPages.TryGetValue(date, out BlogPage blogPage)) {
		return Helpers.HtmlResult(notFoundHtml, 404);
	}

	byte[] html = Helpers.SelectCompressionAndAppendHeaders(context, blogPage.Html, blogPage.BrotliHtml);
	Helpers.AppendCacheControl(context, TimeSpan.FromDays(7));
	return Helpers.HtmlResult(html);
});

app.MapGet("/vn/{book:regex(^[A-Za-z]{{1,100}}$)}", (string book, HttpContext context) =>
{
	if (!visualPulps.TryGetValue(book, out VisualNovel pulp)) {
		return Helpers.HtmlResult(notFoundHtml, 404);
	}

	byte[] html = Helpers.SelectCompressionAndAppendHeaders(context, pulp.Html, pulp.BrotliHtml);
	Helpers.AppendCacheControl(context, TimeSpan.FromDays(7));
	return Helpers.HtmlResult(html);
});

app.MapGet("/vn/{book:regex(^[A-Za-z]{{1,100}}$)}/images/{image:regex(^[a-z0-9-]{{1,100}}$)}.webp", async (string book, string image, HttpContext context) =>
{
	if (visualPulps.TryGetValue(book, out VisualNovel pulp)) {
		context.Response.ContentType = "image/webp";
		string path = Path.Combine(baseDirectory, "VisualPulps", pulp.DirName, "images", image + ".webp");

		try {
			context.Response.StatusCode = 200;
			Helpers.AppendCacheControl(context, TimeSpan.FromDays(30));
			await context.Response.SendFileAsync(path);
			return Results.Empty;
		} catch (FileNotFoundException) { }
	}

	context.Response.StatusCode = 404;
	return Results.Empty;
});

app.MapFallback((HttpContext context) =>
{
	if (HttpMethods.IsGet(context.Request.Method)) {
		return Helpers.HtmlResult(notFoundHtml, 404);
	}
	return Results.NotFound();
});

app.Run();