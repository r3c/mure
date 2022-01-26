namespace Mure
{
	public interface ICompiler<TPattern, TValue>
	{
		ICompiler<TPattern, TValue> AddEndOfFile(TValue value);

		ICompiler<TPattern, TValue> AddPattern(TPattern pattern, TValue value);

		IMatcher<TValue> Compile();
	}
}
