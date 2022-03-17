namespace Mure.Peg
{
	public readonly struct PegError
	{
		public static PegError CreateUnknownStateKey(string key)
		{
			return new PegError { UnknownStateKey = key };
		}

		public readonly string? UnknownStateKey { get; init; }
	}
}
