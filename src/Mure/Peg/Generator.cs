using Mure.Peg.Generators;

namespace Mure.Peg
{
	static class Generator
	{
		public static IGenerator CreateCSharp(PegDefinition definition)
		{
			return new CSharpGenerator(definition);
		}
	}
}
