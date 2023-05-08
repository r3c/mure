namespace Mure;

public interface ICompiler<in TPattern, TValue>
{
	ICompiler<TPattern, TValue> AddEndOfFile(TValue value);

	ICompiler<TPattern, TValue> AddPattern(TPattern pattern, TValue value);

	IMatcher<TValue> Compile();
}
