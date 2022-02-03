using System.Collections.Generic;

namespace Mure.Peg
{
	readonly struct PegState
	{
		public readonly IReadOnlyDictionary<string, PegAction> Actions;
		public readonly PegOperation Operation;

		public PegState(PegOperation operation, IReadOnlyDictionary<string, PegAction> actions)
		{
			Actions = actions;
			Operation = operation;
		}
	}
}
