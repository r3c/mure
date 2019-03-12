using Mure.Scanners;

namespace Mure
{
	public interface ICompiler<TInput, TValue>
	{
		IScanner<TValue> Compile(TInput input);
	}
}