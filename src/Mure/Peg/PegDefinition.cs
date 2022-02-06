using System.Collections.Generic;

namespace Mure.Peg
{
	readonly struct PegDefinition
	{
		public readonly IReadOnlyList<PegState> States;

		public PegDefinition(IReadOnlyList<PegState> states)
		{
			States = states;
		}
	}
}
