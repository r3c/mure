using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mure.Automata;
using Mure.Compilers.Pattern;
using Mure.Matchers;

namespace Mure.Compilers
{
	internal static class RegexCompiler
	{
		public static readonly IMatcher<Lexem> Matcher;

		static RegexCompiler()
		{
			var automata = new NonDeterministicAutomata<Lexem>();
			var escape = automata.PushEmpty();

			escape.BranchTo('(', '(', automata.PushValue(new Lexem(LexemType.Escape, '(')));
			escape.BranchTo(')', ')', automata.PushValue(new Lexem(LexemType.Escape, ')')));
			escape.BranchTo('*', '*', automata.PushValue(new Lexem(LexemType.Escape, '*')));
			escape.BranchTo('+', '+', automata.PushValue(new Lexem(LexemType.Escape, '+')));
			escape.BranchTo('-', '-', automata.PushValue(new Lexem(LexemType.Escape, '-')));
			escape.BranchTo('.', '.', automata.PushValue(new Lexem(LexemType.Escape, '.')));
			escape.BranchTo('?', '?', automata.PushValue(new Lexem(LexemType.Escape, '?')));
			escape.BranchTo('[', '[', automata.PushValue(new Lexem(LexemType.Escape, '[')));
			escape.BranchTo(']', ']', automata.PushValue(new Lexem(LexemType.Escape, ']')));
			escape.BranchTo('\\', '\\', automata.PushValue(new Lexem(LexemType.Escape, '\\')));
			escape.BranchTo('^', '^', automata.PushValue(new Lexem(LexemType.Escape, '^')));
			escape.BranchTo('{', '{', automata.PushValue(new Lexem(LexemType.Escape, '{')));
			escape.BranchTo('|', '|', automata.PushValue(new Lexem(LexemType.Escape, '|')));
			escape.BranchTo('}', '}', automata.PushValue(new Lexem(LexemType.Escape, '}')));
			escape.BranchTo('n', 'n', automata.PushValue(new Lexem(LexemType.Escape, '\n')));
			escape.BranchTo('r', 'r', automata.PushValue(new Lexem(LexemType.Escape, '\r')));
			escape.BranchTo('t', 't', automata.PushValue(new Lexem(LexemType.Escape, '\t')));

			var character = automata.PushEmpty();
			var literal = automata.PushValue(new Lexem(LexemType.Literal));

			character.BranchTo(-1, -1, automata.PushValue(new Lexem(LexemType.End)));
			character.BranchTo(char.MinValue, '\'', literal);
			character.BranchTo('(', '(', automata.PushValue(new Lexem(LexemType.SequenceBegin)));
			character.BranchTo(')', ')', automata.PushValue(new Lexem(LexemType.SequenceEnd)));
			character.BranchTo('*', '*', automata.PushValue(new Lexem(LexemType.ZeroOrMore)));
			character.BranchTo('+', '+', automata.PushValue(new Lexem(LexemType.OneOrMore)));
			character.BranchTo(',', ',', automata.PushValue(new Lexem(LexemType.Comma)));
			character.BranchTo('-', '-', automata.PushValue(new Lexem(LexemType.Range)));
			character.BranchTo('.', '.', automata.PushValue(new Lexem(LexemType.Wildcard)));
			character.BranchTo('/', '/', literal);
			character.BranchTo('0', '9', automata.PushValue(new Lexem(LexemType.Digit)));
			character.BranchTo(':', '>', literal);
			character.BranchTo('?', '?', automata.PushValue(new Lexem(LexemType.ZeroOrOne)));
			character.BranchTo('@', 'Z', literal);
			character.BranchTo('[', '[', automata.PushValue(new Lexem(LexemType.ClassBegin)));
			character.BranchTo('\\', '\\', escape);
			character.BranchTo(']', ']', automata.PushValue(new Lexem(LexemType.ClassEnd)));
			character.BranchTo('^', '^', automata.PushValue(new Lexem(LexemType.Negate)));
			character.BranchTo('_', 'z', literal);
			character.BranchTo('{', '{', automata.PushValue(new Lexem(LexemType.RepeatBegin)));
			character.BranchTo('|', '|', automata.PushValue(new Lexem(LexemType.Alternative)));
			character.BranchTo('}', '}', automata.PushValue(new Lexem(LexemType.RepeatEnd)));
			character.BranchTo('~', char.MaxValue, literal);

			Matcher = new AutomataMatcher<Lexem>(character.ToDeterministic());
		}

		public static Node Match(IMatchIterator<Lexem> iterator)
		{
			var match = NextOrThrow(iterator);
			var (node, _) = MatchAlternative(iterator, match, true);

			return node;
		}

		private static Exception CreateException(string message, int position)
		{
			return new ArgumentException($"{message} at position {position}");
		}

		private static (Node, Match<Lexem>) MatchAlternative(IMatchIterator<Lexem> iterator, Match<Lexem> match, bool atTopLevel)
		{
			var alternativeNodes = new List<Node>();

			while (true)
			{
				var (sequenceNodes, nextMatch) = MatchSequence(iterator, match, atTopLevel);

				alternativeNodes.Add(sequenceNodes);

				if (nextMatch.Value.Type != LexemType.Alternative)
					return (Node.CreateAlternative(alternativeNodes), nextMatch);

				match = NextOrThrow(iterator);
			}
		}

		private static Node MatchClass(IMatchIterator<Lexem> iterator, Match<Lexem> match)
		{
			var negated = false;
			var ranges = new List<NodeRange>();

			// Allow first character of a class to be special "negate class" character
			if (match.Value.Type == LexemType.Negate)
			{
				negated = true;

				match = NextOrThrow(iterator);
			}

			// Allow first (or post-negate) character of a class to be literal "end of class" character
			if (match.Value.Type == LexemType.ClassEnd)
			{
				ranges.Add(new NodeRange(match.Capture[0], match.Capture[0]));

				match = NextOrThrow(iterator);
			}

			while (true)
			{
				// Match next character, which may later be considered as the
				// beginning character of range
				char begin;
				char end;

				switch (match.Value.Type)
				{
					case LexemType.End:
						throw CreateException("unfinished characters class", iterator.Position);

					case LexemType.ClassEnd:
						return Node.CreateCharacter(negated ? NegateRanges(ranges) : ranges);

					case LexemType.Escape:
						begin = match.Value.Replacement;

						break;

					default:
						begin = match.Capture[0];

						break;
				}

				match = NextOrThrow(iterator);

				// If next lexem defines a range (e.g. "a-z"), read next one to
				// get end character for this range before registering it
				if (match.Value.Type == LexemType.Range)
				{
					match = NextOrThrow(iterator);
					end = match.Value.Type switch
					{
						LexemType.End => throw CreateException("unfinished characters class", iterator.Position),
						LexemType.Escape => match.Value.Replacement,
						_ => match.Capture[0]
					};

					match = NextOrThrow(iterator);
				}

				// Otherwise register transition from a single character
				else
					end = begin;

				ranges.Add(new NodeRange(begin, end));
			}
		}

		private static (int min, int max) MatchRepeat(IMatchIterator<Lexem> iterator, Match<Lexem> match)
		{
			var buffer = new StringBuilder();

			while (match.Value.Type == LexemType.Digit)
			{
				buffer.Append(match.Capture[0]);

				match = NextOrThrow(iterator);
			}

			int max;
			var min = buffer.Length > 0 ? int.Parse(buffer.ToString()) : 0;

			if (match.Value.Type == LexemType.Comma)
			{
				buffer.Clear();

				match = NextOrThrow(iterator);

				while (match.Value.Type == LexemType.Digit)
				{
					buffer.Append(match.Capture[0]);

					match = NextOrThrow(iterator);
				}

				max = buffer.Length > 0 ? int.Parse(buffer.ToString()) : -1;

				if (max >= 0 && max < min)
					throw CreateException("invalid repeat sequence", iterator.Position);
			}
			else
				max = min;

			if (match.Value.Type != LexemType.RepeatEnd)
				throw CreateException("expected end of repeat specifier", iterator.Position);

			return (min, max);
		}

		private static (Node, Match<Lexem>) MatchSequence(IMatchIterator<Lexem> iterator, Match<Lexem> match, bool atTopLevel)
		{
			var sequenceNodes = new List<Node>();

			while (true)
			{
				// Match literal character or character class
				Match<Lexem> nextMatch;
				Node node;

				switch (match.Value.Type)
				{
					case LexemType.Alternative:
						return (Node.CreateSequence(sequenceNodes), match);

					case LexemType.End:
						if (!atTopLevel)
							throw CreateException("unfinished parenthesis", iterator.Position);

						return (Node.CreateSequence(sequenceNodes), match);

					case LexemType.ClassBegin:
						node = MatchClass(iterator, NextOrThrow(iterator));
						nextMatch = NextOrThrow(iterator);

						break;

					case LexemType.Escape:
						node = Node.CreateCharacter(match.Value.Replacement);
						nextMatch = NextOrThrow(iterator);

						break;

					case LexemType.SequenceBegin:
						var (alternativeNode, alternativeNextMatch) = MatchAlternative(iterator, NextOrThrow(iterator), false);

						node = alternativeNode;
						nextMatch = alternativeNextMatch;

						break;

					case LexemType.SequenceEnd:
						if (!atTopLevel)
							return (Node.CreateSequence(sequenceNodes), NextOrThrow(iterator));

						node = Node.CreateCharacter(match.Capture[0]);
						nextMatch = NextOrThrow(iterator);

						break;

					case LexemType.Wildcard:
						node = Node.CreateCharacter(new[] { new NodeRange(char.MinValue, char.MaxValue) });
						nextMatch = NextOrThrow(iterator);

						break;

					default:
						node = Node.CreateCharacter(match.Capture[0]);
						nextMatch = NextOrThrow(iterator);

						break;
				}

				// Match repeat specifier if any
				int max;
				int min;

				switch (nextMatch.Value.Type)
				{
					case LexemType.OneOrMore:
						(min, max) = (1, -1);
						match = NextOrThrow(iterator);

						break;

					case LexemType.RepeatBegin:
						(min, max) = MatchRepeat(iterator, NextOrThrow(iterator));
						match = NextOrThrow(iterator);

						break;

					case LexemType.ZeroOrMore:
						(min, max) = (0, -1);
						match = NextOrThrow(iterator);

						break;

					case LexemType.ZeroOrOne:
						(min, max) = (0, 1);
						match = NextOrThrow(iterator);

						break;

					default:
						(min, max) = (1, 1);
						match = nextMatch;

						break;
				}

				sequenceNodes.Add(Node.CreateRepeat(node, min, max));
			}
		}

		private static IReadOnlyList<NodeRange> NegateRanges(IEnumerable<NodeRange> ranges)
		{
			var negatedRanges = new List<NodeRange>();
			var orderedRanges = ranges.OrderBy(range => range.Begin).ToList();
			var previous = char.MinValue;

			foreach (var range in orderedRanges)
			{
				if (previous < range.Begin)
					negatedRanges.Add(new NodeRange(previous, (char)(range.Begin - 1)));

				previous = range.End < char.MaxValue ? (char)(range.End + 1) : char.MaxValue;
			}

			if (previous < char.MaxValue)
				negatedRanges.Add(new NodeRange(previous, char.MaxValue));

			return negatedRanges;
		}

		private static Match<Lexem> NextOrThrow(IMatchIterator<Lexem> iterator)
		{
			if (!iterator.TryMatchNext(out var match))
				throw CreateException("unrecognized character", iterator.Position);

			return match;
		}
	}

	internal class RegexCompiler<TValue> : PatternCompiler<TValue>
	{
		public RegexCompiler() :
			base(RegexCompiler.Matcher)
		{
		}

		protected override Node CreateGraph(IMatchIterator<Lexem> iterator)
		{
			return RegexCompiler.Match(iterator);
		}
	}
}
