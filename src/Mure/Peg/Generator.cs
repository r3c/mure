using Mure.Peg.Generators;

namespace Mure.Peg
{
	static class Generator
	{
		public const string CSharpName = "csharp";

		public static IGenerator CreateCSharp(PegDefinition definition)
		{
			return new CSharpGenerator(CSharpName, definition);
		}
	}
}
