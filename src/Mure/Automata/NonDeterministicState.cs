using System.Collections.Generic;

namespace Mure.Automata
{
	internal readonly struct NonDeterministicState<TValue>
	{
		public readonly List<Branch> Branches;
		public readonly List<int> Epsilons;
		public readonly bool HasValue;
		public readonly TValue? Value;

		public NonDeterministicState(TValue? value, bool hasValue)
		{
			Branches = new List<Branch>();
			Epsilons = new List<int>();
			HasValue = hasValue;
			Value = value;
		}
	}
}
