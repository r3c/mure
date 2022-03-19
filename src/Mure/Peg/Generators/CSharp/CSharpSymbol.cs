namespace Mure.Peg.Generators.CSharp
{
	internal readonly struct CSharpSymbol
	{
		public static string SanitizeIdentifier(string raw)
		{
			return raw; // FIXME
		}

		public readonly string Identifier;
		public readonly string Type;

		public CSharpSymbol(string type, string identifier)
		{
			Identifier = identifier;
			Type = type;
		}

		public string FormatDeclaration()
		{
			return $"{Type} {SanitizeIdentifier(Identifier)}";
		}
	}
}
