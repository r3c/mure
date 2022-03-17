using System.Collections.Generic;

namespace Mure.Peg
{
	readonly struct PegDefinition
	{
		public readonly string ContextType;
		public readonly string StartKey;
		public readonly IReadOnlyList<PegState> States;

		public PegDefinition(string contextType, string startKey, IReadOnlyList<PegState> states)
		{
			ContextType = contextType;
			StartKey = startKey;
			States = states;
		}
	}
}
