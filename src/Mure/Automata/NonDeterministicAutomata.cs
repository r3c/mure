﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Mure.Automata;

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

	public int PushEmpty()
	{
		var index = _states.Count;

		_states.Add(new NonDeterministicState<TValue>(default, false));

		return index;
	}

	public int PushValue(TValue value)
	{
		var index = _states.Count;

		_states.Add(new NonDeterministicState<TValue>(value, true));

		return index;
	}

	public ConversionResult<DeterministicAutomata<TValue>, TValue> ToDeterministic(int index)
	{
		var states = new List<DeterministicState<TValue>>();
		var start = GetOrConvertStates(states, new List<Equivalence>(), new[] { index });

		if (start.Error != ConversionError.None)
			return new ConversionResult<DeterministicAutomata<TValue>, TValue>(start.Error, default, start.Values);

		var automata = new DeterministicAutomata<TValue>(states, start.Result);

		return ConversionResult<DeterministicAutomata<TValue>, TValue>.Success(automata);
	}

	/// <Summary>
	/// https://www.geeksforgeeks.org/theory-of-computation-conversion-from-nfa-to-dfa/
	/// </Summary>
	private ConversionResult<int, TValue> ConvertStates(List<DeterministicState<TValue>> states,
		List<Equivalence> equivalences, IReadOnlyList<int> indices)
	{
		// Create new state, save it to known states and connect to child states
		var stateIndex = CreateState(states, indices);

		if (stateIndex.Error != ConversionError.None)
			return stateIndex;

		equivalences.Add(new Equivalence(indices, stateIndex.Result));

		var branches = indices
			.SelectMany(GetAllBranchesOf)
			.OrderBy(branch => branch, BranchComparer.Instance)
			.ToList();

		for (var i = 0; i < branches.Count;)
		{
			var branch = branches[i];
			IReadOnlyList<int> targets;
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
					targets = branches.Skip(i).Take(last - i).Select(b => b.Target).Distinct().ToList();
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
			var state = states[stateIndex.Result];

			if (state.Branches.Count > 0 && branch.Begin <= state.Branches.Last().End)
				throw new ArgumentOutOfRangeException(nameof(branch.Begin), branch.Begin, "range overlap");

			var target = GetOrConvertStates(states, equivalences, targets);

			if (target.Error != ConversionError.None)
				return target;

			state.Branches.Add(new Branch(branch.Begin, end, target.Result));
		}

		return stateIndex;
	}

	/// <Summary>
	/// Create new deterministic state equivalent to given set of input
	/// non-deterministic ones.
	/// </Summary>
	private ConversionResult<int, TValue> CreateState(ICollection<DeterministicState<TValue>> states,
		IEnumerable<int> indices)
	{
		var values = indices.SelectMany(GetAllValuesOf).ToArray();

		if (values.Length > 1)
			return ConversionResult<int, TValue>.Collision(values);

		var state = values.Length > 0 ? new DeterministicState<TValue>(values[0], true) : new DeterministicState<TValue>(default, false);
		var index = states.Count;

		states.Add(state);

		return ConversionResult<int, TValue>.Success(index);
	}

	private IEnumerable<Branch> GetAllBranchesOf(int index)
	{
		return GetAllTargetsOf(index)
			.SelectMany(targetIndex => _states[targetIndex].Branches);
	}

	private IEnumerable<int> GetAllTargetsOf(int index)
	{
		return _states[index].Epsilons
			.SelectMany(GetAllTargetsOf)
			.Concat(new[] { index })
			.Distinct();
	}

	private IEnumerable<TValue> GetAllValuesOf(int index)
	{
		return GetAllTargetsOf(index)
			.Where(targetIndex => _states[targetIndex].HasValue)
			.Select(targetIndex => _states[targetIndex].Value!);
	}

	/// <Summary>
	/// Find deterministic state matching the exact set of input
	/// non-deterministic states in currently saved states, if any, or
	/// start conversion of a new one otherwise.
	/// </Summary>
	private ConversionResult<int, TValue> GetOrConvertStates(List<DeterministicState<TValue>> states,
		List<Equivalence> equivalences, IReadOnlyList<int> indices)
	{
		var index = equivalences.FindIndex(equivalence => equivalence.Sources.SetEquals(indices));

		return index >= 0
			// Match from previous conversion was found: return it unchanged
			? ConversionResult<int, TValue>.Success(equivalences[index].Target)
			// No match was found: create a new one
			: ConvertStates(states, equivalences, indices);
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
