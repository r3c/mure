namespace Mure
{
	public interface ICompiler<TPattern, TValue>
	{
		ICompiler<TPattern, TValue> Associate(TPattern pattern, TValue value);

		IMatcher<TValue> Compile();
	}
}
