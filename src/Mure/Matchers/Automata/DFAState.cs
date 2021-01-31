using System;
using System.Collections.Generic;
using System.Linq;

namespace Mure.Matchers.Automata
{
	class DFAState<TValue>
	{
		private static readonly BranchComparer Comparer = new BranchComparer();

		public IReadOnlyList<Branch<DFAState<TValue>>> Branches => _branches;

		public readonly bool HasValue;
		public readonly TValue Value;

		private readonly List<Branch<DFAState<TValue>>> _branches = new List<Branch<DFAState<TValue>>>();

		public DFAState(TValue value)
		{
			HasValue = true;
			Value = value;
		}

		public DFAState()
		{
			HasValue = false;
			Value = default;
		}

		public void ConnectTo(int begin, int end, DFAState<TValue> next)
		{
			if (_branches.Count > 0 && begin <= _branches.Last().End)
				throw new ArgumentOutOfRangeException(nameof(begin), begin, "range overlap");

			_branches.Add(new Branch<DFAState<TValue>>(begin, end, next));
		}

		public bool TryFollow(int key, out DFAState<TValue> next)
		{
			var index = _branches.BinarySearch(new Branch<DFAState<TValue>>(key, default, default), Comparer);

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

		private class BranchComparer : IComparer<Branch<DFAState<TValue>>>
		{
			public int Compare(Branch<DFAState<TValue>> x, Branch<DFAState<TValue>> y)
			{
				return x.Begin.CompareTo(y.Begin);
			}
		}
	}
}
