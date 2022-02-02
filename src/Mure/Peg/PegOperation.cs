using System;
using System.Collections.Generic;

namespace Mure.Peg
{
	readonly struct PegOperation
	{
		public static PegOperation CreateCharacterSet(IReadOnlyList<PegRange> ranges)
		{
			return new PegOperation(PegOperator.CharacterSet, Array.Empty<PegReference>(), ranges);
		}

		public static PegOperation CreateChoice(IReadOnlyList<PegReference> references)
		{
			return new PegOperation(PegOperator.Choice, references, Array.Empty<PegRange>());
		}

		public static PegOperation CreateOneOrMore(PegReference reference)
		{
			return new PegOperation(PegOperator.OneOrMore, new[] { reference }, Array.Empty<PegRange>());
		}

		public static PegOperation CreateSequence(IReadOnlyList<PegReference> references)
		{
			return new PegOperation(PegOperator.Sequence, references, Array.Empty<PegRange>());
		}

		public static PegOperation CreateZeroOrMore(PegReference reference)
		{
			return new PegOperation(PegOperator.ZeroOrMore, new[] { reference }, Array.Empty<PegRange>());
		}

		public static PegOperation CreateZeroOrOne(PegReference reference)
		{
			return new PegOperation(PegOperator.ZeroOrOne, new[] { reference }, Array.Empty<PegRange>());
		}

		public readonly IReadOnlyList<PegRange> CharacterRanges;
		public readonly PegOperator Operator;
		public readonly IReadOnlyList<PegReference> References;

		private PegOperation(PegOperator op, IReadOnlyList<PegReference> references, IReadOnlyList<PegRange> characterRanges)
		{
			CharacterRanges = characterRanges;
			Operator = op;
			References = references;
		}
	}
}
