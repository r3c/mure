using System.Collections.Generic;

namespace Mure.Peg
{
	readonly struct PegDefinition
	{
		public readonly int StartIndex;
		public readonly IReadOnlyList<PegState> States;

		public PegDefinition(IReadOnlyList<PegState> states, int startIndex)
		{
			StartIndex = startIndex;
			States = states;
		}
	}
}
