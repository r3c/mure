namespace Mure.Compilers.Regular
{
	enum LexemType
	{
		End,
		Alternative,
		ClassBegin,
		ClassEnd,
		Comma,
		Digit,
		Escape,
		Literal,
		Negate,
		OneOrMore,
		Range,
		RepeatBegin,
		RepeatEnd,
		SequenceBegin,
		SequenceEnd,
		Wildcard,
		ZeroOrMore,
		ZeroOrOne
	}
}
