using System;

namespace Mure.Peg.Generators.CSharp
{
	readonly struct CSharpImplementation
	{
		public readonly Func<CSharpGenerator, PegOperation, string> Infer;
		public readonly Func<CSharpGenerator, CSharpWriter, CSharpSymbol, PegOperation, string, string?, PegError?> Write;

		public CSharpImplementation(Func<CSharpGenerator, PegOperation, string> infer, Func<CSharpGenerator, CSharpWriter, CSharpSymbol, PegOperation, string, string?, PegError?> write)
		{
			Infer = infer;
			Write = write;
		}
	}
}
