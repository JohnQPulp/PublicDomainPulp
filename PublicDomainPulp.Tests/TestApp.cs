using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Pulp.PublicDomainPulp.Tests;

internal class TestApp : WebApplicationFactory<Program> {
	private static readonly TestApp Instance = new();

	public static HttpClient Client => Instance.CreateClient();
}

internal static class AssertHelpers {
	extension(HttpResponseMessage res) {
		public void AssertContentType(string contentType, bool shouldHaveUtf8Charset) {
			MediaTypeHeaderValue header = res.Content.Headers.ContentType!;
			Assert.AreEqual(contentType, header.MediaType, "Mismatch on expected content type.");
			if (shouldHaveUtf8Charset) {
				Assert.AreEqual("utf-8", header.CharSet, "Mismatch on expected utf-8 charset.");
			}
		}

		public async Task AssertHasTitle() {
			string body = await res.Content.ReadAsStringAsync();
			Assert.IsTrue(Regex.IsMatch(body, "<title>.+</title>"), "Expected body content to have a populated title tag.");
		}
	}
}