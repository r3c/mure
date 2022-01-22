namespace Mure.Automata
{
	internal readonly struct NonDeterministicNode<TValue>
	{
		private readonly NonDeterministicAutomata<TValue> _automata;

		private readonly int _index;

		public NonDeterministicNode(NonDeterministicAutomata<TValue> automata, int index)
		{
			_automata = automata;
			_index = index;
		}

		public void BranchTo(int begin, int end, NonDeterministicNode<TValue> target)
		{
			_automata.BranchTo(_index, begin, end, target._index);
		}

		public void EpsilonTo(NonDeterministicNode<TValue> target)
		{
			_automata.EpsilonTo(_index, target._index);
		}

		public DeterministicAutomata<TValue> ToDeterministic()
		{
			return _automata.ToDeterministic(_index);
		}
	}
}
