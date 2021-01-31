using System;
using System.Collections.Generic;
using System.Linq;

namespace Mure.MatchIterators.Automata
{
	class DeterministicState<TValue>
	{
		private static readonly BranchComparer Comparer = new BranchComparer();

		public IReadOnlyList<Branch<DeterministicState<TValue>>> Branches => _branches;

		public readonly bool HasValue;
		public readonly TValue Value;

		private readonly List<Branch<DeterministicState<TValue>>> _branches = new List<Branch<DeterministicState<TValue>>>();

		public DeterministicState(TValue value)
		{
			HasValue = true;
			Value = value;
		}

		public DeterministicState()
		{
			HasValue = false;
			Value = default;
		}

		public void ConnectTo(int begin, int end, DeterministicState<TValue> next)
		{
			if (_branches.Count > 0 && begin <= _branches.Last().End)
				throw new ArgumentOutOfRangeException(nameof(begin), begin, "range overlap");

			_branches.Add(new Branch<DeterministicState<TValue>>(begin, end, next));
		}

		public bool TryFollow(int key, out DeterministicState<TValue> next)
		{
			var index = _branches.BinarySearch(new Branch<DeterministicState<TValue>>(key, default, default), Comparer);

			if (index >= 0)
			{
				next = _branches[index].Value;

				return true;
			}

			index = ~index;

			if (index > 0 && _branches[index - 1].Begin <= key && key <= _branches[index - 1].End)
			{
				next = _branches[index - 1].Value;

				return true;
			}

			next = default;

			return false;
		}

		private class BranchComparer : IComparer<Branch<DeterministicState<TValue>>>
		{
			public int Compare(Branch<DeterministicState<TValue>> x, Branch<DeterministicState<TValue>> y)
			{
				return x.Begin.CompareTo(y.Begin);
			}
		}
	}
}
