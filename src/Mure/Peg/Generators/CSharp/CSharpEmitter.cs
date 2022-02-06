using System;

namespace Mure.Peg.Generators.CSharp
{
	readonly struct CSharpEmitter
	{
		public readonly Func<CSharpGenerator, PegOperation, string> Infer;
		public readonly Action<CSharpGenerator, CSharpWriter, PegOperation, string, string?> Write;

		public CSharpEmitter(Func<CSharpGenerator, PegOperation, string> infer, Action<CSharpGenerator, CSharpWriter, PegOperation, string, string?> write)
		{
			Infer = infer;
			Write = write;
		}
	}
}
