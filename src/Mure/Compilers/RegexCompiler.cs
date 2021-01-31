using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Mure.Compilers.Regex;
using Mure.Matchers;
using Mure.MatchIterators.Automata;

namespace Mure.Compilers
{
	static class RegexCompiler
	{
		public static readonly IMatcher<Lexem> Scanner;

		static RegexCompiler()
		{
			var literal = new NonDeterministicState<Lexem>(new Lexem(LexemType.Literal));
			var escape = new NonDeterministicState<Lexem>();

			escape.ConnectTo('(', '(', new NonDeterministicState<Lexem>(new Lexem(LexemType.Special, '(')));
			escape.ConnectTo(')', ')', new NonDeterministicState<Lexem>(new Lexem(LexemType.Special, ')')));
			escape.ConnectTo('*', '*', new NonDeterministicState<Lexem>(new Lexem(LexemType.Special, '*')));
			escape.ConnectTo('+', '+', new NonDeterministicState<Lexem>(new Lexem(LexemType.Special, '+')));
			escape.ConnectTo('-', '-', new NonDeterministicState<Lexem>(new Lexem(LexemType.Special, '-')));
			escape.ConnectTo('.', '.', new NonDeterministicState<Lexem>(new Lexem(LexemType.Special, '.')));
			escape.ConnectTo('?', '?', new NonDeterministicState<Lexem>(new Lexem(LexemType.Special, '?')));
			escape.ConnectTo('[', '[', new NonDeterministicState<Lexem>(new Lexem(LexemType.Special, '[')));
			escape.ConnectTo(']', ']', new NonDeterministicState<Lexem>(new Lexem(LexemType.Special, ']')));
			escape.ConnectTo('\\', '\\', new NonDeterministicState<Lexem>(new Lexem(LexemType.Special, '\\')));
			escape.ConnectTo('^', '^', new NonDeterministicState<Lexem>(new Lexem(LexemType.Special, '^')));
			escape.ConnectTo('{', '{', new NonDeterministicState<Lexem>(new Lexem(LexemType.Special, '{')));
			escape.ConnectTo('|', '|', new NonDeterministicState<Lexem>(new Lexem(LexemType.Special, '|')));
			escape.ConnectTo('}', '}', new NonDeterministicState<Lexem>(new Lexem(LexemType.Special, '}')));
			escape.ConnectTo('n', 'n', new NonDeterministicState<Lexem>(new Lexem(LexemType.Special, '\n')));
			escape.ConnectTo('r', 'r', new NonDeterministicState<Lexem>(new Lexem(LexemType.Special, '\r')));
			escape.ConnectTo('t', 't', new NonDeterministicState<Lexem>(new Lexem(LexemType.Special, '\t')));

			var character = new NonDeterministicState<Lexem>();

			character.ConnectTo(-1, -1, new NonDeterministicState<Lexem>(new Lexem(LexemType.End)));
			character.ConnectTo(char.MinValue, ' ', literal);
			character.ConnectTo('!', '\'', literal);
			character.ConnectTo('(', '(', new NonDeterministicState<Lexem>(new Lexem(LexemType.SequenceBegin)));
			character.ConnectTo(')', ')', new NonDeterministicState<Lexem>(new Lexem(LexemType.SequenceEnd)));
			character.ConnectTo('*', '*', new NonDeterministicState<Lexem>(new Lexem(LexemType.ZeroOrMore)));
			character.ConnectTo('+', '+', new NonDeterministicState<Lexem>(new Lexem(LexemType.OneOrMore)));
			character.ConnectTo(',', ',', new NonDeterministicState<Lexem>(new Lexem(LexemType.Comma)));
			character.ConnectTo('-', '-', new NonDeterministicState<Lexem>(new Lexem(LexemType.Range)));
			character.ConnectTo('.', '.', new NonDeterministicState<Lexem>(new Lexem(LexemType.Wildcard)));
			character.ConnectTo('/', '/', literal);
			character.ConnectTo('0', '9', new NonDeterministicState<Lexem>(new Lexem(LexemType.Digit)));
			character.ConnectTo(':', '>', literal);
			character.ConnectTo('?', '?', new NonDeterministicState<Lexem>(new Lexem(LexemType.ZeroOrOne)));
			character.ConnectTo('@', 'Z', literal);
			character.ConnectTo('[', '[', new NonDeterministicState<Lexem>(new Lexem(LexemType.ClassBegin)));
			character.ConnectTo('\\', '\\', escape);
			character.ConnectTo(']', ']', new NonDeterministicState<Lexem>(new Lexem(LexemType.ClassEnd)));
			character.ConnectTo('^', '^', new NonDeterministicState<Lexem>(new Lexem(LexemType.Negate)));
			character.ConnectTo('_', 'z', literal);
			character.ConnectTo('{', '{', new NonDeterministicState<Lexem>(new Lexem(LexemType.RepeatBegin)));
			character.ConnectTo('|', '|', new NonDeterministicState<Lexem>(new Lexem(LexemType.Alternative)));
			character.ConnectTo('}', '}', new NonDeterministicState<Lexem>(new Lexem(LexemType.RepeatEnd)));
			character.ConnectTo('~', char.MaxValue, literal);

			Scanner = new AutomataMatcher<Lexem>(character.ConvertToDeterministic());
		}

		public static Node MatchAlternative(IMatchIterator<Lexem> matcher, Match<Lexem> match, bool atTopLevel)
		{
			var alernatives = new List<List<Node>>();
			var sequence = new List<Node>();

			alernatives.Add(sequence);

			while (true)
			{
				// Match literal character or character class
				Node node;

				switch (match.Value.Type)
				{
					case LexemType.Alternative:
						match = NextOrThrow(matcher);
						sequence = new List<Node>();

						alernatives.Add(sequence);

						continue;

					case LexemType.ClassBegin:
						node = Node.CreateCharacter(MatchClass(matcher, NextOrThrow(matcher)));

						break;

					case LexemType.End:
						if (atTopLevel)
							return Node.CreateAlternative(alernatives);

						throw new ArgumentException("unfinished sequence");

					case LexemType.SequenceBegin:
						node = MatchAlternative(matcher, NextOrThrow(matcher), false);

						break;

					case LexemType.SequenceEnd:
						if (!atTopLevel)
							return Node.CreateAlternative(alernatives);

						node = Node.CreateCharacter(match.Capture[0]);

						break;

					case LexemType.Special:
						node = Node.CreateCharacter(match.Value.Special);

						break;

					case LexemType.Wildcard:
						node = Node.CreateCharacter(new[] { new NodeRange(char.MinValue, char.MaxValue) });

						break;

					default:
						node = Node.CreateCharacter(match.Capture[0]);

						break;
				}

				match = NextOrThrow(matcher);

				// Match repeat specifier if any
				int max;
				int min;

				switch (match.Value.Type)
				{
					case LexemType.OneOrMore:
						(min, max) = (1, -1);

						break;

					case LexemType.RepeatBegin:
						(min, max) = MatchRepeat(matcher, NextOrThrow(matcher));

						break;

					case LexemType.ZeroOrMore:
						(min, max) = (0, -1);

						break;

					case LexemType.ZeroOrOne:
						(min, max) = (0, 1);

						break;

					default:
						sequence.Add(node);

						continue;
				}

				match = NextOrThrow(matcher);

				sequence.Add(Node.CreateRepeat(node, min, max));
			}
		}

		public static IReadOnlyList<NodeRange> MatchClass(IMatchIterator<Lexem> matcher, Match<Lexem> match)
		{
			if (match.Value.Type == LexemType.Negate)
				throw new NotImplementedException("negated character classes are not supported yet");

			var ranges = new List<NodeRange>();

			// Allow first character of a class to be literal "end of class" character
			if (match.Value.Type == LexemType.ClassEnd)
			{
				ranges.Add(new NodeRange(match.Capture[0], match.Capture[0]));

				match = NextOrThrow(matcher);
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
						throw new ArgumentException("unfinished characters class");

					case LexemType.ClassEnd:
						return ranges;

					case LexemType.Special:
						begin = match.Value.Special;

						break;

					default:
						begin = match.Capture[0];

						break;
				}

				match = NextOrThrow(matcher);

				// If next lexem defines a range (e.g. "a-z"), read next one to
				// get end character for this range before registering it
				if (match.Value.Type == LexemType.Range)
				{
					match = NextOrThrow(matcher);

					switch (match.Value.Type)
					{
						case LexemType.End:
							throw new ArgumentException("unfinished characters class");

						case LexemType.Special:
							end = match.Value.Special;

							break;

						default:
							end = match.Capture[0];

							break;

					}

					match = NextOrThrow(matcher);
				}

				// Otherwise register transition from a single character
				else
					end = begin;

				ranges.Add(new NodeRange(begin, end));
			}
		}

		public static (int min, int max) MatchRepeat(IMatchIterator<Lexem> matcher, Match<Lexem> match)
		{
			var buffer = new StringBuilder();

			while (match.Value.Type == LexemType.Digit)
			{
				buffer.Append(match.Capture[0]);

				match = NextOrThrow(matcher);
			}

			int max;
			var min = buffer.Length > 0 ? int.Parse(buffer.ToString()) : 0;

			if (match.Value.Type == LexemType.Comma)
			{
				buffer.Clear();

				match = NextOrThrow(matcher);

				while (match.Value.Type == LexemType.Digit)
				{
					buffer.Append(match.Capture[0]);

					match = NextOrThrow(matcher);
				}

				max = buffer.Length > 0 ? int.Parse(buffer.ToString()) : -1;

				if (max >= 0 && max < min)
					throw new ArgumentException("invalid repeat sequence");
			}
			else
				max = min;

			if (match.Value.Type != LexemType.RepeatEnd)
				throw new ArgumentException("expected end of repeat specifier");

			return (min, max);
		}

		public static Match<Lexem> NextOrThrow(IMatchIterator<Lexem> matcher)
		{
			if (!matcher.TryMatchNext(out var match))
				throw new ArgumentException("unrecognized character");

			return match;
		}
	}

	class RegexCompiler<TValue> : ICompiler<IEnumerable<(string, TValue)>, TValue>
	{
		public IMatcher<TValue> Compile(IEnumerable<(string, TValue)> input)
		{
			var state = new NonDeterministicState<TValue>();

			foreach (var search in input)
				state.EpsilonTo(CompilePattern(search.Item1, search.Item2));

			return new AutomataMatcher<TValue>(state.ConvertToDeterministic());
		}

		/// <Summary>
		/// Compile regular expression pattern into graph of non-deterministic
		/// states leading to given value.
		/// </Summary>
		private static NonDeterministicState<TValue> CompilePattern(string pattern, TValue value)
		{
			var start = new NonDeterministicState<TValue>();

			using (var reader = new StringReader(pattern))
			{
				var matcher = RegexCompiler.Scanner.Open(reader);
				var node = RegexCompiler.MatchAlternative(matcher, RegexCompiler.NextOrThrow(matcher), true);
				var state = node.ConvertToState(start);

				state.EpsilonTo(new NonDeterministicState<TValue>(value));
			}

			return start;
		}
	}
}
