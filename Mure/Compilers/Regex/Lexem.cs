
namespace Mure.Compilers.Regex
{
	readonly struct Lexem
	{
		public readonly char Special;
		public readonly LexemType Type;

		public Lexem(LexemType type, char special)
		{
			Special = special;
			Type = type;
		}

		public Lexem(LexemType type)
		{
			Special = default;
			Type = type;
		}
	}
}
