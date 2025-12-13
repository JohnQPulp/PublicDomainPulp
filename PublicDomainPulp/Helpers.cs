using System.Reflection;

namespace Pulp.PublicDomainPulp;

internal static class Helpers {
	public static readonly string HeadHtml = ReadResource("head.html");
	
	public static string ReadResource(string name)
	{
		Assembly asm = Assembly.GetExecutingAssembly();
		using Stream stream = asm.GetManifestResourceStream("Pulp.PublicDomainPulp." + name);
		using StreamReader reader = new StreamReader(stream);
		return reader.ReadToEnd();
	}
}