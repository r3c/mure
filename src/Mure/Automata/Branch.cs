namespace Mure.Automata
{
	internal readonly struct Branch
	{
		public readonly int Begin;
		public readonly int End;
		public readonly int Target;

		public Branch(int begin, int end, int target)
		{
			Begin = begin;
			End = end;
			Target = target;
		}
	}
}
