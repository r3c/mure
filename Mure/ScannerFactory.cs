using System.Collections.Generic;
using Mure.Compilers;

namespace Mure
{
	public static class ScannerFactory
	{
		public static IScanner<TValue> CreateRegex<TValue>(IEnumerable<(string, TValue)> patterns)
		{
			var compiler = new RegexCompiler<TValue>();

			return compiler.Compile(patterns);
		}
	}
}