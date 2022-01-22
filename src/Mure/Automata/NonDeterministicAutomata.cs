﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Mure.Automata
{
	internal class NonDeterministicAutomata<TValue>
	{
		private readonly List<NonDeterministicState<TValue>> _states = new();

		public void BranchTo(int source, int begin, int end, int target)
		{
			_states[source].Branches.Add(new Branch(begin, end, target));
		}

		public void EpsilonTo(int source, int target)
		{
			if (source == target)
				return;

			_states[source].Epsilons.Add(target);
		}

		public int Push(NonDeterministicState<TValue> state)
		{
			var index = _states.Count;

			_states.Add(state);

			return index;
		}

		public (DeterministicAutomata<TValue>, int) ToDeterministic(int index)
		{
			var output = new DeterministicAutomata<TValue>();
			var start = GetOrConvertState(output, new[] { index }, new List<Equivalence>());

			return (output, start);
		}

		/// <Summary>
		/// https://www.geeksforgeeks.org/theory-of-computation-conversion-from-nfa-to-dfa/
		/// </Summary>
		private void ConnectToStates(DeterministicAutomata<TValue> output, int index, IReadOnlyList<int> indices, List<Equivalence> equivalences)
		{
			var branches = indices
				.SelectMany(GetAllBranchesOf)
				.OrderBy(branch => branch.Begin)
				.ToList();

			for (var i = 0; i < branches.Count;)
			{
				var branch = branches[i];
				int[] targets;
				int end;

				// No next branch or no overlap between current branch and next one
				if (i + 1 >= branches.Count || branch.End < branches[i + 1].Begin)
				{
					// Use branch as is and move to next branch for next iteration
					targets = new[] { branch.Target };
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
						targets = new[] { branch.Target };
						end = next.Begin - 1;

						// Align starting range with the one from next branch for next iteration
						branches[i] = new Branch(next.Begin, branch.End, branch.Target);
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
						targets = branches.Skip(i).Take(last - i).Select(b => b.Target).Distinct().ToArray();
						end = lowest;

						// Update starting range of overlapped branches from selection
						while (last-- > i)
						{
							// Range of overlapped branch was entirely included in current selection, branch can be safely removed
							if (end >= branches[last].End)
								branches.RemoveAt(last);

							// Otherwise shift its starting range after the ending range of current selection
							else
								branches[last] = new Branch(end + 1, branches[last].End, branches[last].Target);
						}
					}
				}

				// Recursively convert target states to deterministic and connect current one to it
				output.ConnectTo(index, branch.Begin, end, GetOrConvertState(output, targets, equivalences));
			}
		}

		/// <Summary>
		/// Create new deterministic state equivalent to given set of input
		/// non-deterministic ones.
		/// </Summary>
		private int CreateState(DeterministicAutomata<TValue> output, IEnumerable<int> indices)
		{
			var values = indices.SelectMany(index => GetAllValuesOf(index)).ToArray();

			if (values.Length > 1)
				throw new InvalidOperationException($"transition collision between multiple values: {string.Join(", ", values)}");

			return values.Length > 0 ? output.PushValue(values[0]) : output.PushEmpty();
		}

		private IEnumerable<Branch> GetAllBranchesOf(int index)
		{
			return GetAllTargetsOf(index).SelectMany(index => _states[index].Branches);
		}

		private IEnumerable<int> GetAllTargetsOf(int index)
		{
			return _states[index].Epsilons.SelectMany(target => GetAllTargetsOf(target)).Concat(new[] { index }).Distinct();
		}

		private IEnumerable<TValue> GetAllValuesOf(int index)
		{
			return GetAllTargetsOf(index).Where(index => _states[index].HasValue).Select(index => _states[index].Value);
		}

		/// <Summary>
		/// Find deterministic state matching the exact set of input
		/// non-deterministic states in currently saved states, if any, or
		/// start conversion of a new one otherwise.
		/// </Summary>
		private int GetOrConvertState(DeterministicAutomata<TValue> output, IReadOnlyList<int> indices, List<Equivalence> equivalences)
		{
			var index = equivalences.FindIndex(equivalence => equivalence.Sources.SetEquals(indices));

			// Match from previous conversion was found: return it unchanged
			if (index >= 0)
				return equivalences[index].Target;

			// No match was found: create new state, save it to known states and connect to child states
			var result = CreateState(output, indices);

			equivalences.Add(new Equivalence(indices, result));

			ConnectToStates(output, result, indices, equivalences);

			return result;
		}

		private readonly struct Equivalence
		{
			public readonly IReadOnlySet<int> Sources;
			public readonly int Target;

			public Equivalence(IEnumerable<int> sources, int target)
			{
				Sources = sources.ToHashSet();
				Target = target;
			}
		}
	}
}
