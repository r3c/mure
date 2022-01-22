namespace Mure.Automata
{
	internal readonly struct NonDeterministicNode<TValue>
	{
		public static NonDeterministicNode<TValue> Create()
		{
			var automata = new NonDeterministicAutomata<TValue>();

			return new NonDeterministicNode<TValue>(automata, automata.Push(new NonDeterministicState<TValue>(default, false)));
		}

		private readonly NonDeterministicAutomata<TValue> _automata;

		private readonly int _index;

		private NonDeterministicNode(NonDeterministicAutomata<TValue> automata, int index)
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

		public NonDeterministicNode<TValue> PushEmpty()
		{
			return new NonDeterministicNode<TValue>(_automata, _automata.Push(new NonDeterministicState<TValue>(default, false)));
		}

		public NonDeterministicNode<TValue> PushValue(TValue value)
		{
			return new NonDeterministicNode<TValue>(_automata, _automata.Push(new NonDeterministicState<TValue>(value, true)));
		}

		public (DeterministicAutomata<TValue>, int) ToDeterministicNode()
		{
			return _automata.ToDeterministic(_index);
		}
	}
}
