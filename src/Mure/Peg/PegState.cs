using System.Collections.Generic;

namespace Mure.Peg
{
	readonly struct PegState
	{
		public readonly IReadOnlyDictionary<string, PegAction> Actions;
		public readonly string Key;
		public readonly PegOperation Operation;

		public PegState(string key, PegOperation operation, IReadOnlyDictionary<string, PegAction> actions)
		{
			Actions = actions;
			Key = key;
			Operation = operation;
		}
	}
}
