namespace Mure.Compilers.Pattern;

internal readonly struct NodeRange
{
	public readonly char Begin;
	public readonly char End;

	public NodeRange(char begin, char end)
	{
		Begin = begin;
		End = end;
	}
}