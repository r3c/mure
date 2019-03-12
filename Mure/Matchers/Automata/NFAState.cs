using System;
using System.Collections.Generic;
using System.Linq;

namespace Mure.Matchers.Automata
{
	class NFAState<TValue>
	{
		private IEnumerable<Branch<NFAState<TValue>>> AllBranches => GetAllStates().SelectMany(e => e._branches);
		private IEnumerable<TValue> AllValues => GetAllStates().Where(e => e._hasValue).Select(e => e._value);

		private readonly List<Branch<NFAState<TValue>>> _branches = new List<Branch<NFAState<TValue>>>();
		private readonly List<NFAState<TValue>> _epsilons = new List<NFAState<TValue>>();
		private readonly bool _hasValue;
		private readonly TValue _value;

		public NFAState(TValue value)
		{
			_hasValue = true;
			_value = value;
		}

		public NFAState()
		{
			_hasValue = false;
			_value = default;
		}

		public DFAState<TValue> ConvertToDFA()
		{
			return GetOrConvertState(new[] { this }, new List<Equivalence>());
		}

		public void ConnectTo(int begin, int end, NFAState<TValue> target)
		{
			_branches.Add(new Branch<NFAState<TValue>>(begin, end, target));
		}

		public void EpsilonTo(NFAState<TValue> target)
		{
			_epsilons.Add(target);
		}

		private IEnumerable<NFAState<TValue>> GetAllStates()
		{
			var self = new[] { this };

			return _epsilons.Except(self).SelectMany(e => e.GetAllStates()).Concat(self).Distinct();
		}

		/// <Summary>
		/// https://www.geeksforgeeks.org/theory-of-computation-conversion-from-nfa-to-dfa/
		/// </Summary>
		private static void ConnectToStates(DFAState<TValue> result, IReadOnlyList<NFAState<TValue>> states, List<Equivalence> equivalences)
		{
			var branches = states
				.SelectMany(n => n.AllBranches)
				.OrderBy(pair => pair.Begin)
				.ToList();

			for (var i = 0; i < branches.Count;)
			{
				var branch = branches[i];
				NFAState<TValue>[] targets;
				int end;

				// No next branch or no overlap between current branch and next one
				if (i + 1 >= branches.Count || branch.End < branches[i + 1].Begin)
				{
					// Use branch as is and move to next branch for next iteration
					targets = new[] { branch.Value };
					end = branch.End;

					++i;
				}

				// There is an overlap with next branch that requires some range shifting
				else
				{
					var next = branches[i + 1];

					// Current branch has a non-null exclusive part
					if (branch.Begin < next.Begin)
					{
						// Extract exclusive part from current branch
						targets = new[] { branch.Value };
						end = next.Begin - 1;

						// Align starting range with the one from next branch for next iteration
						branches[i] = new Branch<NFAState<TValue>>(next.Begin, branch.End, branch.Value);
					}

					// Current branch shares starting range with next one(s)
					else
					{
						// Select all sibling branches with same starting range and keep lowest ending range
						var last = i + 1;
						var lowest = branch.End;

						while (last < branches.Count && branch.Begin == branches[last].Begin)
						{
							lowest = Math.Min(branches[last].End, lowest);

							++last;
						}

						// Prevent current range selection from overlapping next branch
						if (last < branches.Count)
							lowest = Math.Min(branches[last].Begin - 1, lowest);

						// Use selected branches and range selection (use "ToArray" to force evaluation before source array is modified)
						targets = branches.Skip(i).Take(last - i).Select(b => b.Value).Distinct().ToArray();
						end = lowest;

						// Update starting range of overlapped branches from selection
						while (last-- > i)
						{
							// Range of overlapped branch was entirely included in current selection, branch can be safely removed
							if (end >= branches[last].End)
								branches.RemoveAt(last);

							// Otherwise shift its starting range after the ending range of current selection
							else
								branches[last] = new Branch<NFAState<TValue>>(end + 1, branches[last].End, branches[last].Value);
						}
					}
				}

				// Recursively convert target states to DFA state and connect current one to it
				result.ConnectTo(branch.Begin, end, GetOrConvertState(targets, equivalences));
			}
		}

		/// <Summary>
		/// Create new DFA state equivalent to given set of input NFA states.
		/// </Summary>
		private static DFAState<TValue> CreateState(IEnumerable<NFAState<TValue>> states)
		{
			var values = states.SelectMany(n => n.AllValues).ToArray();

			if (values.Length > 1)
				throw new InvalidOperationException($"transition collision between multiple values: {string.Join(", ", values)}");

			return values.Length > 0 ? new DFAState<TValue>(values[0]) : new DFAState<TValue>();
		}

		/// <Summary>
		/// Find DFA state matching the exact set of input NFA states in
		/// currently saved states, if any, or start conversion of a new one
		/// otherwise.
		/// </Summary>
		private static DFAState<TValue> GetOrConvertState(IReadOnlyList<NFAState<TValue>> states, List<Equivalence> equivalences)
		{
			var index = equivalences.FindIndex(state => state.Sources.Count == states.Count() && state.Sources.All(source => states.Any(n => object.ReferenceEquals(n, source))));

			// Match from previous conversion was found: return it unchanged
			if (index >= 0)
				return equivalences[index].Target;

			// No match was found: create new state, save it to known states and connect to child states
			var result = CreateState(states);

			equivalences.Add(new Equivalence(states, result));

			ConnectToStates(result, states, equivalences);

			return result;
		}

		private struct Equivalence
		{
			public readonly DFAState<TValue> Target;
			public readonly IReadOnlyList<NFAState<TValue>> Sources;

			public Equivalence(IReadOnlyList<NFAState<TValue>> sources, DFAState<TValue> target)
			{
				Target = target;
				Sources = sources;
			}
		}
	}
}
