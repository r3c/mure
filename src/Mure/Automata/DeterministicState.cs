using System.Collections.Generic;

namespace Mure.Automata
{
	internal readonly struct DeterministicState<TValue>
	{
		public readonly List<Branch> Branches;
		public readonly bool HasValue;
		public readonly TValue? Value;

		public DeterministicState(TValue? value, bool hasValue)
		{
			Branches = new List<Branch>();
			HasValue = hasValue;
			Value = value;
		}
	}
}
