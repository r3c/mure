using System;
using System.Collections.Generic;
using System.Linq;

namespace Mure.Automata
{
	internal class DeterministicAutomata<TValue>
	{
		public IReadOnlyList<DeterministicState<TValue>> States => _states;

		private readonly List<DeterministicState<TValue>> _states = new();

		public int PushEmpty()
		{
			var index = _states.Count;

			_states.Add(new DeterministicState<TValue>(default, false));

			return index;
		}

		public int PushValue(TValue value)
		{
			var index = _states.Count;

			_states.Add(new DeterministicState<TValue>(value, true));

			return index;
		}

		public void ConnectTo(int index, int begin, int end, int next)
		{
			var state = _states[index];

			if (state.Branches.Count > 0 && begin <= state.Branches.Last().End)
				throw new ArgumentOutOfRangeException(nameof(begin), begin, "range overlap");

			state.Branches.Add(new Branch(begin, end, next));
		}

		public bool TryFollow(int index, int key, out int next)
		{
			var state = _states[index];
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
			var state = _states[index];

			value = state.Value;

			return state.HasValue;
		}
	}
}
