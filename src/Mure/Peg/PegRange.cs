namespace Mure.Peg
{
	readonly struct PegRange
	{
		public readonly char Begin;
		public readonly char End;

		public PegRange(char begin, char end)
		{
			Begin = begin;
			End = end;
		}
	}
}
