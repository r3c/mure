using System.Collections.Generic;
using Mure.Compilers;

namespace Mure
{
	public static class Matcher
	{
		public static IMatcher<TValue> CreateFromRegex<TValue>(IEnumerable<(string, TValue)> patterns)
		{
			var compiler = new RegexCompiler<TValue>();

			return compiler.Compile(patterns);
		}
	}
}
