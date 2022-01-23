using Mure.Compilers;

namespace Mure
{
	public static class Compiler
	{
		public static ICompiler<string, TValue> CreateFromGlob<TValue>()
		{
			return new GlobCompiler<TValue>();
		}

		public static ICompiler<string, TValue> CreateFromRegex<TValue>()
		{
			return new RegexCompiler<TValue>();
		}
	}
}
