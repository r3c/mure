using System;
using System.Collections.Generic;
using System.Linq;
using Mure.Matchers.Automata;

namespace Mure.CLI
{
	class Program
	{
		static void Main(string[] args)
		{
			var q0 = new NFAState<int>();
			var q1 = new NFAState<int>();
			var q2 = new NFAState<int>(17);

			q0.ConnectTo('a', 'b', q0);
			q0.ConnectTo('a', 'a', q1);
			q1.ConnectTo('b', 'b', q2);

			PrintDFA(q0.ConvertToDFA(), 0, new List<Tuple<DFAState<int>, int>>());
		}

		private static void PrintDFA<TValue>(DFAState<TValue> state, int indent, List<Tuple<DFAState<TValue>, int>> identifiers)
		{
			var identifier = identifiers.Count + 1;
			var prefix = string.Join(string.Empty, Enumerable.Repeat("  ", indent));

			identifiers.Add(Tuple.Create(state, identifier));

			Console.WriteLine($"{prefix}#{identifier}{(state.HasValue ? $" = {state.Value}" : string.Empty)}");

			foreach (var branch in state.Branches)
			{
				var previous = identifiers.FirstOrDefault(i => object.ReferenceEquals(i.Item1, branch.Value));
				var range = branch.Begin == branch.End ? $"{branch.Begin}" : $"[{branch.Begin}-{branch.End}]";

				Console.WriteLine($"{prefix} - {range} -> {(previous != null ? $"#{previous.Item2}" : string.Empty)}");

				if (previous == null)
					PrintDFA(branch.Value, indent + 1, identifiers);
			}
		}
	}
}
