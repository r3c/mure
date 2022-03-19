namespace Mure.Peg
{
	readonly struct PegConfiguration
	{
		public readonly string? ContextName;
		public readonly string? ContextType;
		public readonly string? Preamble;

		public PegConfiguration(string? preamble, string? contextType, string? contextName)
		{
			ContextName = contextName;
			ContextType = contextType;
			Preamble = preamble;
		}
	}
}
