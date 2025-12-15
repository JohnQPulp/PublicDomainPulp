using System.Reflection;

namespace Pulp.PublicDomainPulp;

internal static class Helpers {
	public static readonly string HeadHtml = ReadResource("snippets.head.html");
	public static readonly string VNBodyHtml = ReadResource("snippets.vn-body.html");
	public static readonly string HomeBodyHtml = ReadResource("snippets.home-body.html");
	
	public static string ReadResource(string name)
	{
		Assembly asm = Assembly.GetExecutingAssembly();
		using Stream stream = asm.GetManifestResourceStream("Pulp.PublicDomainPulp." + name);
		using StreamReader reader = new StreamReader(stream);
		return reader.ReadToEnd();
	}
}