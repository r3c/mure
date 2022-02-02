namespace Mure.Peg
{
	readonly struct PegState
	{
		public readonly PegOperation Operation;
		public readonly string? Type;

		public PegState(PegOperation operation, string? type)
		{
			Operation = operation;
			Type = type;
		}
	}
}
