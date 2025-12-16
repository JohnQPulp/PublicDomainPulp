using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace Pulp.PublicDomainPulp.Tests;

[TestClass]
public class StartupTests {
	[TestMethod]
	[DataRow("/")]
	[DataRow("/about")]
	[DataRow("/About")]
	public async Task HomePages_200(string page) {
		HttpResponseMessage res = await TestApp.Client.GetAsync(page);
		res.EnsureSuccessStatusCode();
		res.AssertContentType("text/html", true);
		await res.AssertHasTitle();
	}

	[TestMethod]
	[DataRow("/foo")]
	[DataRow("/vn/foo/pulp.html")]
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
	[DataRow("/vn/CupOfGold/pulp.css")]
	[DataRow("/assets/favicon.icon")]
	[DataRow("/assets/home.scss")]
	public async Task BadFiles_404(string page) {
		HttpResponseMessage res = await TestApp.Client.GetAsync(page);
		Assert.AreEqual(HttpStatusCode.NotFound, res.StatusCode);
		res.AssertEmpty();
	}

	[TestMethod]
	[DataRow("/assets/favicon.ico", "image/x-icon", false)]
	[DataRow("/assets/home.css", "text/css", true)]
	[DataRow("/assets/vn.js", "application/javascript", true)]
	public async Task Assets_200(string path, string contentType, bool shouldHaveUtf8Charset) {
		HttpResponseMessage res = await TestApp.Client.GetAsync(path);
		res.EnsureSuccessStatusCode();
		res.AssertContentType(contentType, shouldHaveUtf8Charset);

		string cacheControl = res.Headers.CacheControl.ToString();
		Assert.AreEqual($"public, max-age={60 * 60}, immutable", cacheControl);
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
			"/vn/cupofgold/pulp.html"
		);
		if (acceptEncoding != null) {
			request.Headers.Add("Accept-Encoding", acceptEncoding);
		}
		HttpResponseMessage res = await TestApp.Client.SendAsync(request);
		res.EnsureSuccessStatusCode();
		res.AssertContentType("text/html", true);

		Assert.AreEqual(contentEncoding, res.Content.Headers.ContentEncoding.SingleOrDefault());
		await res.AssertHasTitle();

		string cacheControl = res.Headers.CacheControl.ToString();
		Assert.AreEqual($"public, max-age={60 * 60}, immutable", cacheControl);
	}

	[TestMethod]
	public async Task VN_Compresses() {
		HttpRequestMessage request = new(
			HttpMethod.Get,
			"/vn/cupofgold/pulp.html"
		);
		HttpResponseMessage res = await TestApp.Client.SendAsync(request);
		int length1 = (int)res.Content.Headers.ContentLength;
		string html1 = await TestHelpers.GetText(res);

		request = new(
			HttpMethod.Get,
			"/vn/cupofgold/pulp.html"
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

		string cacheControl = res.Headers.CacheControl.ToString();
		Assert.AreEqual($"public, max-age={60 * 60 * 24 * 7}, immutable", cacheControl);

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
		await res.AssertEmpty();
	}
}