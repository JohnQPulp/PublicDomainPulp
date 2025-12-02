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
}