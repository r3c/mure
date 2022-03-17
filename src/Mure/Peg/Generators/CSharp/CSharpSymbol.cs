namespace Mure.Peg.Generators.CSharp
{
	internal readonly struct CSharpSymbol
	{
		public readonly string Identifier;
		public readonly string Type;

		public CSharpSymbol(string type, string identifier)
		{
			Identifier = identifier;
			Type = type;
		}
	}
}
