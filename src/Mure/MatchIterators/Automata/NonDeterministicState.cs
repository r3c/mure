using System;
using System.Collections.Generic;
using System.Linq;

namespace Mure.MatchIterators.Automata
{
	class NonDeterministicState<TValue>
	{
		private IEnumerable<Branch<NonDeterministicState<TValue>>> AllBranches => GetAllStates().SelectMany(e => e._branches);
		private IEnumerable<TValue> AllValues => GetAllStates().Where(e => e._hasValue).Select(e => e._value);

		private readonly List<Branch<NonDeterministicState<TValue>>> _branches = new List<Branch<NonDeterministicState<TValue>>>();
		private readonly List<NonDeterministicState<TValue>> _epsilons = new List<NonDeterministicState<TValue>>();
		private readonly bool _hasValue;
		private readonly TValue _value;

		public NonDeterministicState(TValue value)
		{
			_hasValue = true;
			_value = value;
		}

		public NonDeterministicState()
		{
			_hasValue = false;
			_value = default;
		}

		public DeterministicState<TValue> ConvertToDeterministic()
		{
			return GetOrConvertState(new[] { this }, new List<Equivalence>());
		}

		public void ConnectTo(int begin, int end, NonDeterministicState<TValue> target)
		{
			_branches.Add(new Branch<NonDeterministicState<TValue>>(begin, end, target));
		}

		public void EpsilonTo(NonDeterministicState<TValue> target)
		{
			_epsilons.Add(target);
		}

		private IEnumerable<NonDeterministicState<TValue>> GetAllStates()
		{
			var self = new[] { this };

			return _epsilons.Except(self).SelectMany(e => e.GetAllStates()).Concat(self).Distinct();
		}

		/// <Summary>
		/// https://www.geeksforgeeks.org/theory-of-computation-conversion-from-nfa-to-dfa/
		/// </Summary>
		private static void ConnectToStates(DeterministicState<TValue> result, IReadOnlyList<NonDeterministicState<TValue>> states, List<Equivalence> equivalences)
		{
			var branches = states
				.SelectMany(n => n.AllBranches)
				.OrderBy(pair => pair.Begin)
				.ToList();

			for (var i = 0; i < branches.Count;)
			{
				var branch = branches[i];
				NonDeterministicState<TValue>[] targets;
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
						branches[i] = new Branch<NonDeterministicState<TValue>>(next.Begin, branch.End, branch.Value);
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
								branches[last] = new Branch<NonDeterministicState<TValue>>(end + 1, branches[last].End, branches[last].Value);
						}
					}
				}

				// Recursively convert target states to deterministic and connect current one to it
				result.ConnectTo(branch.Begin, end, GetOrConvertState(targets, equivalences));
			}
		}

		/// <Summary>
		/// Create new deterministic state equivalent to given set of input
		/// non-deterministic ones.
		/// </Summary>
		private static DeterministicState<TValue> CreateState(IEnumerable<NonDeterministicState<TValue>> states)
		{
			var values = states.SelectMany(n => n.AllValues).ToArray();

			if (values.Length > 1)
				throw new InvalidOperationException($"transition collision between multiple values: {string.Join(", ", values)}");

			return values.Length > 0 ? new DeterministicState<TValue>(values[0]) : new DeterministicState<TValue>();
		}

		/// <Summary>
		/// Find deterministic state matching the exact set of input
		/// non-deterministic states in currently saved states, if any, or
		/// start conversion of a new one otherwise.
		/// </Summary>
		private static DeterministicState<TValue> GetOrConvertState(IReadOnlyList<NonDeterministicState<TValue>> states, List<Equivalence> equivalences)
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
			public readonly IReadOnlyList<NonDeterministicState<TValue>> Sources;
			public readonly DeterministicState<TValue> Target;

			public Equivalence(IReadOnlyList<NonDeterministicState<TValue>> sources, DeterministicState<TValue> target)
			{
				Sources = sources;
				Target = target;
			}
		}
	}
}
