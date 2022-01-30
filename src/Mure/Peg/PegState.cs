using System;
using System.Collections.Generic;

namespace Mure.Peg
{
	readonly struct PegState
	{
		public static PegState CreateCharacterSet(IReadOnlyList<PegRange> ranges)
		{
			return new PegState(PegOperator.CharacterSet, Array.Empty<int>(), ranges);
		}

		public static PegState CreateChoice(IReadOnlyList<int> stateIndices)
		{
			return new PegState(PegOperator.Choice, stateIndices, Array.Empty<PegRange>());
		}

		public static PegState CreateOneOrMore(int index)
		{
			return new PegState(PegOperator.OneOrMore, new[] { index }, Array.Empty<PegRange>());
		}

		public static PegState CreateSequence(IReadOnlyList<int> stateIndices)
		{
			return new PegState(PegOperator.Sequence, stateIndices, Array.Empty<PegRange>());
		}

		public static PegState CreateZeroOrMore(int index)
		{
			return new PegState(PegOperator.ZeroOrMore, new[] { index }, Array.Empty<PegRange>());
		}

		public static PegState CreateZeroOrOne(int index)
		{
			return new PegState(PegOperator.ZeroOrOne, new[] { index }, Array.Empty<PegRange>());
		}

		public readonly IReadOnlyList<PegRange> CharacterRanges;
		public readonly PegOperator Operator;
		public readonly IReadOnlyList<int> StateIndices;

		private PegState(PegOperator op, IReadOnlyList<int> stateIndices, IReadOnlyList<PegRange> characterRanges)
		{
			CharacterRanges = characterRanges;
			Operator = op;
			StateIndices = stateIndices;
		}
	}
}
