namespace Mure.Compilers.Pattern;

internal readonly struct Lexem
{
	public readonly char Replacement;
	public readonly LexemType Type;

	public Lexem(LexemType type, char replacement)
	{
		Replacement = replacement;
		Type = type;
	}

	public Lexem(LexemType type)
	{
		Replacement = default;
		Type = type;
	}
}
