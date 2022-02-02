namespace Mure.Peg
{
	readonly struct PegReference
	{
		public readonly string? Identifier;
		public readonly int Index;

		public PegReference(int index, string? identifier)
		{
			Identifier = identifier;
			Index = index;
		}
	}
}
