using System.Collections.Generic;

namespace Mure.Automata
{
	internal readonly struct DeterministicAutomata<TValue>
	{
		public readonly IReadOnlyList<DeterministicState<TValue>> States;
		public readonly int Start;

		public DeterministicAutomata(IReadOnlyList<DeterministicState<TValue>> states, int start)
		{
			States = states;
			Start = start;
		}

		public bool TryFollow(int current, int key, out int next)
		{
			var state = States[current];
			var match = state.Branches.BinarySearch(new Branch(key, default, default), BranchComparer.Instance);

			if (match >= 0)
			{
				next = state.Branches[match].Target;

				return true;
			}

			match = ~match;

			if (match > 0 && state.Branches[match - 1].Begin <= key && key <= state.Branches[match - 1].End)
			{
				next = state.Branches[match - 1].Target;

				return true;
			}

			next = default;

			return false;
		}

		public bool TryGetValue(int index, out TValue value)
		{
			var state = States[index];

			value = state.Value;

			return state.HasValue;
		}
	}
}
