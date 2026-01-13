using System.IO.Compression;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Pulp.PublicDomainPulp.Tests;

internal class TestApp : WebApplicationFactory<Program> {
	private static readonly TestApp Instance = new();

	public static HttpClient Client => Instance.CreateClient();
}

internal static class TestHelpers {
	public static async Task<string> GetText(HttpResponseMessage res) {
		if (string.IsNullOrEmpty(res.Content.Headers.ContentEncoding.SingleOrDefault())) {
			return await res.Content.ReadAsStringAsync();
		}

		byte[] bytes = await res.Content.ReadAsByteArrayAsync();
		using MemoryStream input = new(bytes);
		using MemoryStream output = new();
		using (BrotliStream brotli = new(input, CompressionMode.Decompress))
		{
			brotli.CopyTo(output);
		}
		return Encoding.UTF8.GetString(output.ToArray());
	}
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

		public void AssertCacheControl(string expected) {
			string cacheControl = res.Headers.CacheControl.ToString();
#if !DEBUG
			Assert.AreEqual(expected, cacheControl);
#endif
		}

		public async Task AssertEmpty() {
			Assert.AreEqual(0, res.Content.Headers.ContentLength);
			byte[] body = await res.Content.ReadAsByteArrayAsync();
			Assert.IsEmpty(body);
		}

		public async Task AssertHasTitle() {
			string body = await TestHelpers.GetText(res);
			Assert.IsTrue(Regex.IsMatch(body, "<title>.+</title>"), "Expected body content to have a populated title tag.");
		}
	}
}