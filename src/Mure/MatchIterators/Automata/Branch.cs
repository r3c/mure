namespace Mure.MatchIterators.Automata
{
	readonly struct Branch<T>
	{
		public readonly int Begin;
		public readonly int End;
		public readonly T Value;

		public Branch(int begin, int end, T value)
		{
			Begin = begin;
			End = end;
			Value = value;
		}
	}
}
