using System;
using System.Collections.Generic;

namespace Mure.Peg
{
	readonly struct PegOperation
	{
		public static PegOperation CreateCharacterSet(IReadOnlyList<PegRange> ranges)
		{
			return new PegOperation(PegOperator.CharacterSet, Array.Empty<int>(), ranges);
		}

		public static PegOperation CreateChoice(IReadOnlyList<int> stateIndices)
		{
			return new PegOperation(PegOperator.Choice, stateIndices, Array.Empty<PegRange>());
		}

		public static PegOperation CreateOneOrMore(int index)
		{
			return new PegOperation(PegOperator.OneOrMore, new[] { index }, Array.Empty<PegRange>());
		}

		public static PegOperation CreateSequence(IReadOnlyList<int> stateIndices)
		{
			return new PegOperation(PegOperator.Sequence, stateIndices, Array.Empty<PegRange>());
		}

		public static PegOperation CreateZeroOrMore(int index)
		{
			return new PegOperation(PegOperator.ZeroOrMore, new[] { index }, Array.Empty<PegRange>());
		}

		public static PegOperation CreateZeroOrOne(int index)
		{
			return new PegOperation(PegOperator.ZeroOrOne, new[] { index }, Array.Empty<PegRange>());
		}

		public readonly IReadOnlyList<PegRange> CharacterRanges;
		public readonly PegOperator Operator;
		public readonly IReadOnlyList<int> StateIndices;

		private PegOperation(PegOperator op, IReadOnlyList<int> stateIndices, IReadOnlyList<PegRange> characterRanges)
		{
			CharacterRanges = characterRanges;
			Operator = op;
			StateIndices = stateIndices;
		}
	}
}
