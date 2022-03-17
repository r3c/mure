namespace Mure.Peg
{
	readonly struct PegReference
	{
		public readonly string? Identifier;
		public readonly string Key;

		public PegReference(string key, string? identifier)
		{
			Identifier = identifier;
			Key = key;
		}
	}
}
