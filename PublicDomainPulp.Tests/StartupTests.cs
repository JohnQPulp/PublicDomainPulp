using System.Net;

namespace Pulp.PublicDomainPulp.Tests;

[TestClass]
public class StartupTests {
	[TestMethod]
	public async Task Root_200() {
		HttpResponseMessage res = await TestApp.Client.GetAsync("/");
		res.EnsureSuccessStatusCode();
		res.AssertContentType("text/html", true);
		await res.AssertHasTitle();
	}
	
	[TestMethod]
	public async Task VN_200() {
		HttpResponseMessage res = await TestApp.Client.GetAsync("/vn/cupofgold/pulp.html");
		res.EnsureSuccessStatusCode();
		res.AssertContentType("text/html", true);
		await res.AssertHasTitle();
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
		Assert.AreEqual($"public, max-age={60 * 60 * 24 * 7}", cacheControl);
	}
	
	[TestMethod]
	[DataRow("b-foo.webp")]
	[DataRow("o-foo.webp")]
	[DataRow("c-foo.webp")]
	public async Task VN_Image_404(string image) {
		HttpResponseMessage res = await TestApp.Client.GetAsync("/vn/cupofgold/images/" + image);
		Assert.AreEqual(HttpStatusCode.NotFound, res.StatusCode);
	}
}