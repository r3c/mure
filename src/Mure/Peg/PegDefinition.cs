using System.Collections.Generic;

namespace Mure.Peg
{
	readonly struct PegDefinition
	{
		public readonly string ContextType;
		public readonly int StartIndex;
		public readonly IReadOnlyList<PegState> States;

		public PegDefinition(string contextType, IReadOnlyList<PegState> states, int startIndex)
		{
			ContextType = contextType;
			StartIndex = startIndex;
			States = states;
		}
	}
}
