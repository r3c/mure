using System;
using System.Collections.Generic;
using System.IO;
using Mure.Compilers.Regex;
using Mure.Matchers;
using Mure.MatchIterators.Automata;

namespace Mure.Compilers
{
	static class GlobCompiler
	{
		public static readonly IMatcher<Lexem> Matcher;

		private static readonly Node Wildcard = Node.CreateCharacter(new[] { new NodeRange(char.MinValue, char.MaxValue) });

		static GlobCompiler()
		{
			var automata = new NonDeterministicAutomata<Lexem>();
			var escape = automata.PushEmptyState();

			automata.BranchTo(escape, '*', '*', automata.PushValueState(new Lexem(LexemType.Escape, '*')));
			automata.BranchTo(escape, '?', '?', automata.PushValueState(new Lexem(LexemType.Escape, '?')));
			automata.BranchTo(escape, '[', '[', automata.PushValueState(new Lexem(LexemType.Escape, '[')));
			automata.BranchTo(escape, ']', ']', automata.PushValueState(new Lexem(LexemType.Escape, ']')));
			automata.BranchTo(escape, '\\', '\\', automata.PushValueState(new Lexem(LexemType.Escape, '\\')));

			var character = automata.PushEmptyState();
			var literal = automata.PushValueState(new Lexem(LexemType.Literal));

			automata.BranchTo(character, -1, -1, automata.PushValueState(new Lexem(LexemType.End)));
			automata.BranchTo(character, char.MinValue, ' ', literal);
			automata.BranchTo(character, '!', '!', automata.PushValueState(new Lexem(LexemType.Negate)));
			automata.BranchTo(character, '"', ')', literal);
			automata.BranchTo(character, '*', '*', automata.PushValueState(new Lexem(LexemType.ZeroOrMore)));
			automata.BranchTo(character, '+', ',', literal);
			automata.BranchTo(character, '-', '-', automata.PushValueState(new Lexem(LexemType.Range)));
			automata.BranchTo(character, '.', '>', literal);
			automata.BranchTo(character, '?', '?', automata.PushValueState(new Lexem(LexemType.Wildcard)));
			automata.BranchTo(character, '@', 'Z', literal);
			automata.BranchTo(character, '[', '[', automata.PushValueState(new Lexem(LexemType.ClassBegin)));
			automata.BranchTo(character, '\\', '\\', escape);
			automata.BranchTo(character, ']', ']', automata.PushValueState(new Lexem(LexemType.ClassEnd)));
			automata.BranchTo(character, '^', char.MaxValue, literal);

			Matcher = new AutomataMatcher<Lexem>(automata.ConvertToDeterministic(character));
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
			{
				throw new NotImplementedException("negated character classes are not supported yet");
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

	class GlobCompiler<TValue> : ICompiler<IEnumerable<(string, TValue)>, TValue>
	{
		public IMatcher<TValue> Compile(IEnumerable<(string, TValue)> input)
		{
			var automata = new NonDeterministicAutomata<TValue>();
			var start = automata.PushEmptyState();

			foreach (var search in input)
				CompilePattern(automata, start, search.Item1, search.Item2);

			return new AutomataMatcher<TValue>(automata.ConvertToDeterministic(start));
		}

		/// <Summary>
		/// Compile regular expression pattern into graph of non-deterministic
		/// states leading to given value.
		/// </Summary>
		private static void CompilePattern(NonDeterministicAutomata<TValue> automata, int index, string pattern, TValue value)
		{
			using (var reader = new StringReader(pattern))
			{
				var iterator = GlobCompiler.Matcher.Open(reader);
				var node = GlobCompiler.MatchSequence(iterator, GlobCompiler.NextOrThrow(iterator), true);
				var leaf = node.ConvertToState(automata, index);
				var target = automata.PushValueState(value);

				automata.EpsilonTo(leaf, target);
			}
		}
	}
}
