using System;
using System.Collections.Generic;
using Mure.Automata;
using Mure.Compilers.Pattern;
using Mure.Matchers;

namespace Mure.Compilers
{
	internal static class GlobCompiler
	{
		public static readonly IMatcher<Lexem> Matcher;

		private static readonly Node Wildcard = Node.CreateCharacter(new[] { new NodeRange(char.MinValue, char.MaxValue) });

		static GlobCompiler()
		{
			var automata = new NonDeterministicAutomata<Lexem>();
			var escape = automata.PushEmpty();

			escape.BranchTo('*', '*', automata.PushValue(new Lexem(LexemType.Escape, '*')));
			escape.BranchTo('?', '?', automata.PushValue(new Lexem(LexemType.Escape, '?')));
			escape.BranchTo('[', '[', automata.PushValue(new Lexem(LexemType.Escape, '[')));
			escape.BranchTo(']', ']', automata.PushValue(new Lexem(LexemType.Escape, ']')));
			escape.BranchTo('\\', '\\', automata.PushValue(new Lexem(LexemType.Escape, '\\')));

			var character = automata.PushEmpty();
			var literal = automata.PushValue(new Lexem(LexemType.Literal));

			character.BranchTo(-1, -1, automata.PushValue(new Lexem(LexemType.End)));
			character.BranchTo(char.MinValue, ' ', literal);
			character.BranchTo('!', '!', automata.PushValue(new Lexem(LexemType.Negate)));
			character.BranchTo('"', ')', literal);
			character.BranchTo('*', '*', automata.PushValue(new Lexem(LexemType.ZeroOrMore)));
			character.BranchTo('+', ',', literal);
			character.BranchTo('-', '-', automata.PushValue(new Lexem(LexemType.Range)));
			character.BranchTo('.', '>', literal);
			character.BranchTo('?', '?', automata.PushValue(new Lexem(LexemType.Wildcard)));
			character.BranchTo('@', 'Z', literal);
			character.BranchTo('[', '[', automata.PushValue(new Lexem(LexemType.ClassBegin)));
			character.BranchTo('\\', '\\', escape);
			character.BranchTo(']', ']', automata.PushValue(new Lexem(LexemType.ClassEnd)));
			character.BranchTo('^', char.MaxValue, literal);

			var deterministic = character.ToDeterministic();

			if (deterministic.Error != ConversionError.None)
				throw new InvalidOperationException("internal error when initializing glob compiler");

			Matcher = new AutomataMatcher<Lexem>(deterministic.Result);
		}

		public static Node MatchSequence(IMatchIterator<Lexem> iterator, Match<Lexem> match, bool atTopLevel)
		{
			var nodes = new List<Node>();

			while (true)
			{
				// Match literal character or character class
				Node node;

				switch (match.Value.Type)
				{
					case LexemType.ClassBegin:
						node = MatchClass(iterator, NextOrThrow(iterator));

						break;

					case LexemType.End:
						if (atTopLevel)
							return Node.CreateSequence(nodes);

						throw CreateException("unfinished sequence", iterator.Position);

					case LexemType.Escape:
						node = Node.CreateCharacter(match.Value.Replacement);

						break;

					case LexemType.Wildcard:
						node = Wildcard;

						break;

					case LexemType.ZeroOrMore:
						node = Node.CreateRepeat(Wildcard, 0, -1);

						break;

					default:
						node = Node.CreateCharacter(match.Capture[0]);

						break;
				}

				match = NextOrThrow(iterator);

				nodes.Add(node);
			}
		}

		public static Match<Lexem> NextOrThrow(IMatchIterator<Lexem> iterator)
		{
			if (!iterator.TryMatchNext(out var match))
				throw CreateException("unrecognized character", iterator.Position);

			return match;
		}

		private static Exception CreateException(string message, int position)
		{
			return new ArgumentException($"{message} at position {position}");
		}

		private static Node MatchClass(IMatchIterator<Lexem> iterator, Match<Lexem> match)
		{
			var ranges = new List<NodeRange>();

			// Allow first character of a class to be special "negate class" character
			if (match.Value.Type == LexemType.Negate)
				throw new NotImplementedException("negated character classes are not supported yet");

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
						return Node.CreateCharacter(ranges);

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

					switch (match.Value.Type)
					{
						case LexemType.End:
							throw CreateException("unfinished characters class", iterator.Position);

						case LexemType.Escape:
							end = match.Value.Replacement;

							break;

						default:
							end = match.Capture[0];

							break;

					}

					match = NextOrThrow(iterator);
				}

				// Otherwise register transition from a single character
				else
					end = begin;

				ranges.Add(new NodeRange(begin, end));
			}
		}
	}

	internal class GlobCompiler<TValue> : PatternCompiler<TValue>
	{
		public GlobCompiler() :
			base(GlobCompiler.Matcher)
		{
		}

		protected override Node CreateGraph(IMatchIterator<Lexem> iterator)
		{
			return GlobCompiler.MatchSequence(iterator, GlobCompiler.NextOrThrow(iterator), true);
		}
	}
}
