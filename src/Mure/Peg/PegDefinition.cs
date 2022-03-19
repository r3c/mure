using System.Collections.Generic;

namespace Mure.Peg
{
	readonly struct PegDefinition
	{
		public readonly IReadOnlyDictionary<string, PegConfiguration> Configurations;
		public readonly IReadOnlyList<PegState> States;

		public PegDefinition(IReadOnlyDictionary<string, PegConfiguration> configurations, IReadOnlyList<PegState> states)
		{
			Configurations = configurations;
			States = states;
		}
	}
}
