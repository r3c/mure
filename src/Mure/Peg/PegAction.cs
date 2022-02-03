namespace Mure.Peg
{
	readonly struct PegAction
	{
		public readonly string Body;
		public readonly string Type;

		public PegAction(string type, string body)
		{
			Body = body;
			Type = type;
		}
	}
}
