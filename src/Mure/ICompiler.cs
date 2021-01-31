using Mure.Scanners;

namespace Mure
{
	interface ICompiler<TInput, TValue>
	{
		IScanner<TValue> Compile(TInput input);
	}
}
