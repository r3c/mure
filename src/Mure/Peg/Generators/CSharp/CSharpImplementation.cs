using System;

namespace Mure.Peg.Generators.CSharp
{
	readonly struct CSharpImplementation
	{
		public readonly Func<CSharpGenerator, PegOperation, string> Infer;
		public readonly Action<CSharpGenerator, CSharpWriter, CSharpSymbol, PegOperation, string, string?> Write;

		public CSharpImplementation(Func<CSharpGenerator, PegOperation, string> infer, Action<CSharpGenerator, CSharpWriter, CSharpSymbol, PegOperation, string, string?> write)
		{
			Infer = infer;
			Write = write;
		}
	}
}
