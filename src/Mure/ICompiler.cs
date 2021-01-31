namespace Mure
{
	interface ICompiler<TInput, TValue>
	{
		IMatcher<TValue> Compile(TInput input);
	}
}
