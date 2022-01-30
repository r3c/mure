namespace Mure.Peg
{
	readonly struct PegState
	{
		public readonly string? Identifier;
		public readonly PegOperation Operation;

		public PegState(PegOperation operation, string? identifier)
		{
			Identifier = identifier;
			Operation = operation;
		}
	}
}
