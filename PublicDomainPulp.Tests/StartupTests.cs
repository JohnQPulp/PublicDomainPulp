using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;

namespace Pulp.PublicDomainPulp.Tests;

[TestClass]
public class StartupTests {
	[TestMethod]
	[DataRow("/", 60 * 60)]
	[DataRow("/about", 60 * 60 * 24)]
	[DataRow("/About", 60 * 60 * 24)]
	[DataRow("/contact", 60 * 60 * 24)]
	[DataRow("/Contact", 60 * 60 * 24)]
	public async Task HomePages_200(string page, int cacheSeconds) {
		HttpResponseMessage res = await TestApp.Client.GetAsync(page);
		res.EnsureSuccessStatusCode();
		res.AssertContentType("text/html", true);
		await res.AssertHasTitle();
		res.AssertCacheControl($"public, max-age={cacheSeconds}, immutable");
	}

	[TestMethod]
	public async Task StupidMethods_404() {
		HttpMethod[] methods = [HttpMethod.Post, HttpMethod.Put, HttpMethod.Delete, HttpMethod.Head, HttpMethod.Options, HttpMethod.Connect, HttpMethod.Patch, HttpMethod.Trace, HttpMethod.Query];
		foreach (HttpMethod method in methods) {
			HttpRequestMessage request = new(method, "/");
			HttpResponseMessage res = await TestApp.Client.SendAsync(request);
			Assert.AreEqual(HttpStatusCode.NotFound, res.StatusCode);
			res.AssertEmpty();
		}
	}

	[TestMethod]
	[DataRow("/foo")]
	[DataRow("/vn/foo")]
	[DataRow("/blog/2025-12-31")]
	public async Task BadPages_404(string page) {
		HttpResponseMessage res = await TestApp.Client.GetAsync(page);
		Assert.AreEqual(HttpStatusCode.NotFound, res.StatusCode);
		res.AssertContentType("text/html", true);
		await res.AssertHasTitle();

		string content = await res.Content.ReadAsStringAsync();
		Assert.Contains("404 Not Found", content);
	}

	[TestMethod]
	[DataRow("/foo.json")]
	[DataRow("/vn/CupOfGold.css")]
	[DataRow("/vn/CupOfGold.html")]
	[DataRow("/vn/CupOfGold/pulp.css")]
	[DataRow("/vn/CupOfGold/pulp.html")]
	[DataRow("/assets/favicon.icon")]
	[DataRow("/assets/home.scss")]
	public async Task BadFiles_404(string page) {
		HttpResponseMessage res = await TestApp.Client.GetAsync(page);
		Assert.AreEqual(HttpStatusCode.NotFound, res.StatusCode);
		res.AssertEmpty();
	}

	[TestMethod]
	[DataRow("/assets/favicon.ico", "image/x-icon", false)]
	[DataRow("/assets/icon-192.png", "image/png", false)]
	[DataRow("/assets/NotoSerif-Variable-Pulp.woff2", "font/woff2", false)]
	[DataRow("/assets/NotoSerif-Italic-Variable-Pulp.woff2", "font/woff2", false)]
	[DataRow("/assets/preview.avif", "image/avif", false)]
	public async Task Assets_200(string path, string contentType, bool shouldHaveUtf8Charset) {
		HttpResponseMessage res = await TestApp.Client.GetAsync(path);
		res.EnsureSuccessStatusCode();
		res.AssertContentType(contentType, shouldHaveUtf8Charset);
		res.AssertCacheControl($"public, max-age={60 * 60 * 24 * 7}, immutable");
	}

	[TestMethod]
	[DataRow("/assets/icon-192.ico")]
	[DataRow("/assets/favicon.png")]
	public async Task Assets_404(string path) {
		HttpResponseMessage res = await TestApp.Client.GetAsync(path);
		Assert.AreEqual(HttpStatusCode.NotFound, res.StatusCode);
		res.AssertCacheControl("no-store");
		await res.AssertEmpty();
	}

	[TestMethod]
	[DataRow("/images/palette.webp", "image/webp", false)]
	[DataRow("/images/buchan.webp", "image/webp", false)]
	[DataRow("/images/about.webp", "image/webp", false)]
	[DataRow("/images/slide.avif", "image/avif", false)]
	public async Task CC_Images_200(string path, string contentType, bool shouldHaveUtf8Charset) {
		HttpResponseMessage res = await TestApp.Client.GetAsync(path);
		res.EnsureSuccessStatusCode();
		res.AssertContentType(contentType, shouldHaveUtf8Charset);
		res.AssertCacheControl($"public, max-age={60 * 60 * 24 * 30}, immutable");
	}

	[TestMethod]
	[DataRow("/images/reference/b-example.jpeg", "image/jpeg", false)]
	[DataRow("/images/reference/c-example-1.jpeg", "image/jpeg", false)]
	[DataRow("/images/reference/c-example-2.jpeg", "image/jpeg", false)]
	[DataRow("/images/reference/pC.png", "image/png", false)]
	public async Task CC_ImagesInSubfolder_200(string path, string contentType, bool shouldHaveUtf8Charset) {
		HttpResponseMessage res = await TestApp.Client.GetAsync(path);
		res.EnsureSuccessStatusCode();
		res.AssertContentType(contentType, shouldHaveUtf8Charset);
		res.AssertCacheControl($"public, max-age={60 * 60 * 24 * 30}, immutable");
	}

	[TestMethod]
	[DataRow("/images/palette.gif")]
	[DataRow("/images/Buchan.webp")]
	public async Task CC_Images_200(string path) {
		HttpResponseMessage res = await TestApp.Client.GetAsync(path);
		Assert.AreEqual(HttpStatusCode.NotFound, res.StatusCode);
		res.AssertCacheControl("no-store");
		await res.AssertEmpty();
	}

	[TestMethod]
	[DataRow(null, null)]
	[DataRow("", null)]
	[DataRow("gzip", null)]
	[DataRow("gzip, br", "br")]
	[DataRow("gzip, br;q=0.5, deflate", "br")]
	[DataRow("*", "br")]
	public async Task VN_200(string? acceptEncoding, string? contentEncoding) {
		HttpRequestMessage request = new(
			HttpMethod.Get,
			"/vn/cupofgold"
		);
		if (acceptEncoding != null) {
			request.Headers.Add("Accept-Encoding", acceptEncoding);
		}
		HttpResponseMessage res = await TestApp.Client.SendAsync(request);
		res.EnsureSuccessStatusCode();
		res.AssertContentType("text/html", true);

		Assert.AreEqual(contentEncoding, res.Content.Headers.ContentEncoding.SingleOrDefault());
		await res.AssertHasTitle();
		res.AssertCacheControl($"public, max-age={60 * 60 * 24 * 7}, immutable");
	}

	[TestMethod]
	[DataRow("<i>Pride and Prejudice: The Visual&nbsp;Novel</i>")]
	[DataRow("<a href='https://standardebooks.org/ebooks/jane-austen/pride-and-prejudice'>Standard Ebooks .epub source</a>")]
	[DataRow("<a href='https://www.goodreads.com/book/show/14935.Sense_and_Sensibility'>Sense and Sensibility</a>")]
	[DataRow("<a href='https://www.goodreads.com/book/show/5390186-irene-iddesleigh'>Irene Iddesleigh</a>")]
	public async Task VN_ContainsText(string text) {
		HttpResponseMessage res = await TestApp.Client.GetAsync("vn/PrideAndPrejudice");
		string html = await res.Content.ReadAsStringAsync();
		Assert.Contains(text, html);
	}

	[TestMethod]
	public async Task Blog_200() {
		HttpResponseMessage res = await TestApp.Client.GetAsync("/blog/2026-01-01");
		res.EnsureSuccessStatusCode();
		res.AssertContentType("text/html", true);
		await res.AssertHasTitle();
		res.AssertCacheControl($"public, max-age={60 * 60 * 24 * 7}, immutable");
	}

	[TestMethod]
	public async Task VN_Compresses() {
		HttpRequestMessage request = new(
			HttpMethod.Get,
			"/vn/CupOfGold"
		);
		HttpResponseMessage res = await TestApp.Client.SendAsync(request);
		int length1 = (int)res.Content.Headers.ContentLength;
		string html1 = await TestHelpers.GetText(res);

		request = new(
			HttpMethod.Get,
			"/vn/CupOfGold"
		);
		request.Headers.Add("Accept-Encoding", "br");
		res = await TestApp.Client.SendAsync(request);
		int length2 = (int)res.Content.Headers.ContentLength;
		string html2 = await TestHelpers.GetText(res);

		Assert.IsGreaterThan(length2, length1);
		Assert.AreEqual(html1, html2);
	}

	[TestMethod]
	[DataRow("b-title.webp")]
	[DataRow("o-pearl.webp")]
	[DataRow("c-la.webp")]
	public async Task VN_Image_200(string image) {
		HttpResponseMessage res = await TestApp.Client.GetAsync("/vn/cupofgold/images/" + image);
		res.EnsureSuccessStatusCode();
		res.AssertContentType("image/webp", false);
		res.AssertCacheControl($"public, max-age={60 * 60 * 24 * 30}, immutable");

		byte[] bytes =  await res.Content.ReadAsByteArrayAsync();
		byte[] expected = "WEBP"u8.ToArray();
		CollectionAssert.AreEquivalent(expected, bytes.AsSpan(8, 4).ToArray());
	}

	[TestMethod]
	[DataRow("b-foo.webp")]
	[DataRow("o-foo.webp")]
	[DataRow("c-foo.webp")]
	public async Task VN_Image_404(string image) {
		HttpResponseMessage res = await TestApp.Client.GetAsync("/vn/cupofgold/images/" + image);
		Assert.AreEqual(HttpStatusCode.NotFound, res.StatusCode);
		res.AssertCacheControl("no-store");
		await res.AssertEmpty();
	}

	[TestMethod]
	public async Task Pages_Links_200() {
		List<string> pages = ["/", "/about", "/blog", "/upcoming", "/contact"];

		HttpResponseMessage res = await TestApp.Client.GetAsync("/blog");
		string html = await res.Content.ReadAsStringAsync();
		MatchCollection matches = Regex.Matches(html, "<a href='(.*?)'>");
		Assert.IsNotEmpty(matches);
		foreach (Match match in Regex.Matches(html, "<a href='(.*?)'>")) {
			pages.Add(match.Groups[1].Value);
		}

		int imageCount = 0;
		foreach (string page in pages) {
			res = await TestApp.Client.GetAsync(page);
			html = await res.Content.ReadAsStringAsync();
			matches = Regex.Matches(html, "((src)|(href))=[\"'](/.*?)[\"']");
			imageCount += matches.Count;
			foreach (Match match in matches) {
				string link = match.Groups[4].Value;
				res = await TestApp.Client.GetAsync(link);
				Assert.AreEqual(HttpStatusCode.OK, res.StatusCode, $"On page \"{page}\", missing link \"{link}\".");
			}
		}
		Assert.IsGreaterThan(25, imageCount);
	}
}