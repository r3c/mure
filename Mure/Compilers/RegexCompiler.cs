using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Mure.Compilers.Regex;
using Mure.Matchers.Automata;
using Mure.Scanners;

namespace Mure.Compilers
{
	static class RegexCompiler
	{
		public static readonly IScanner<Lexem> Scanner;

		static RegexCompiler()
		{
			var literal = new NFAState<Lexem>(new Lexem(LexemType.Literal));
			var escape = new NFAState<Lexem>();

			escape.ConnectTo('(', '(', new NFAState<Lexem>(new Lexem(LexemType.Special, '(')));
			escape.ConnectTo(')', ')', new NFAState<Lexem>(new Lexem(LexemType.Special, ')')));
			escape.ConnectTo('*', '*', new NFAState<Lexem>(new Lexem(LexemType.Special, '*')));
			escape.ConnectTo('+', '+', new NFAState<Lexem>(new Lexem(LexemType.Special, '+')));
			escape.ConnectTo('-', '-', new NFAState<Lexem>(new Lexem(LexemType.Special, '-')));
			escape.ConnectTo('.', '.', new NFAState<Lexem>(new Lexem(LexemType.Special, '.')));
			escape.ConnectTo('?', '?', new NFAState<Lexem>(new Lexem(LexemType.Special, '?')));
			escape.ConnectTo('[', '[', new NFAState<Lexem>(new Lexem(LexemType.Special, '[')));
			escape.ConnectTo(']', ']', new NFAState<Lexem>(new Lexem(LexemType.Special, ']')));
			escape.ConnectTo('\\', '\\', new NFAState<Lexem>(new Lexem(LexemType.Special, '\\')));
			escape.ConnectTo('^', '^', new NFAState<Lexem>(new Lexem(LexemType.Special, '^')));
			escape.ConnectTo('{', '{', new NFAState<Lexem>(new Lexem(LexemType.Special, '{')));
			escape.ConnectTo('|', '|', new NFAState<Lexem>(new Lexem(LexemType.Special, '|')));
			escape.ConnectTo('}', '}', new NFAState<Lexem>(new Lexem(LexemType.Special, '}')));
			escape.ConnectTo('n', 'n', new NFAState<Lexem>(new Lexem(LexemType.Special, '\n')));
			escape.ConnectTo('r', 'r', new NFAState<Lexem>(new Lexem(LexemType.Special, '\r')));
			escape.ConnectTo('t', 't', new NFAState<Lexem>(new Lexem(LexemType.Special, '\t')));

			var character = new NFAState<Lexem>();

			character.ConnectTo(-1, -1, new NFAState<Lexem>(new Lexem(LexemType.End)));
			character.ConnectTo(char.MinValue, ' ', literal);
			character.ConnectTo('!', '\'', literal);
			character.ConnectTo('(', '(', new NFAState<Lexem>(new Lexem(LexemType.SequenceBegin)));
			character.ConnectTo(')', ')', new NFAState<Lexem>(new Lexem(LexemType.SequenceEnd)));
			character.ConnectTo('*', '*', new NFAState<Lexem>(new Lexem(LexemType.ZeroOrMore)));
			character.ConnectTo('+', '+', new NFAState<Lexem>(new Lexem(LexemType.OneOrMore)));
			character.ConnectTo(',', ',', new NFAState<Lexem>(new Lexem(LexemType.Comma)));
			character.ConnectTo('-', '-', new NFAState<Lexem>(new Lexem(LexemType.Range)));
			character.ConnectTo('.', '.', new NFAState<Lexem>(new Lexem(LexemType.Wildcard)));
			character.ConnectTo('/', '/', literal);
			character.ConnectTo('0', '9', new NFAState<Lexem>(new Lexem(LexemType.Digit)));
			character.ConnectTo(':', '>', literal);
			character.ConnectTo('?', '?', new NFAState<Lexem>(new Lexem(LexemType.ZeroOrOne)));
			character.ConnectTo('@', 'Z', literal);
			character.ConnectTo('[', '[', new NFAState<Lexem>(new Lexem(LexemType.ClassBegin)));
			character.ConnectTo('\\', '\\', escape);
			character.ConnectTo(']', ']', new NFAState<Lexem>(new Lexem(LexemType.ClassEnd)));
			character.ConnectTo('^', '^', new NFAState<Lexem>(new Lexem(LexemType.Negate)));
			character.ConnectTo('_', 'z', literal);
			character.ConnectTo('{', '{', new NFAState<Lexem>(new Lexem(LexemType.RepeatBegin)));
			character.ConnectTo('|', '|', new NFAState<Lexem>(new Lexem(LexemType.Alternative)));
			character.ConnectTo('}', '}', new NFAState<Lexem>(new Lexem(LexemType.RepeatEnd)));
			character.ConnectTo('~', char.MaxValue, literal);

			Scanner = new AutomataScanner<Lexem>(character.ConvertToDFA());
		}

		public static Node MatchAlternative(IMatcher<Lexem> matcher, Match<Lexem> match, bool atTopLevel)
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

		public static IReadOnlyList<NodeRange> MatchClass(IMatcher<Lexem> matcher, Match<Lexem> match)
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

		public static (int min, int max) MatchRepeat(IMatcher<Lexem> matcher, Match<Lexem> match)
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

		public static Match<Lexem> NextOrThrow(IMatcher<Lexem> matcher)
		{
			if (!matcher.TryMatch(out var match))
				throw new ArgumentException("unrecognized character");

			return match;
		}
	}

	public class RegexCompiler<TValue> : ICompiler<IEnumerable<(string, TValue)>, TValue>
	{
		public IScanner<TValue> Compile(IEnumerable<(string, TValue)> input)
		{
			var state = new NFAState<TValue>();

			foreach (var search in input)
				state.EpsilonTo(CompilePattern(search.Item1, search.Item2));

			return new AutomataScanner<TValue>(state.ConvertToDFA());
		}

		/// <Summary>
		/// Compile regular expression pattern into graph of NFA states leading
		/// to given value.
		/// </Summary>
		private static NFAState<TValue> CompilePattern(string pattern, TValue value)
		{
			var start = new NFAState<TValue>();

			using (var reader = new StringReader(pattern))
			{
				var matcher = RegexCompiler.Scanner.Scan(reader);
				var node = RegexCompiler.MatchAlternative(matcher, RegexCompiler.NextOrThrow(matcher), true);
				var state = ConvertToNFA(node, start);

				state.EpsilonTo(new NFAState<TValue>(value));
			}

			return start;
		}

		/// <Summary>
		/// Convert compiled regular expression node into graph of NFA states
		/// connected to given parent state and return final state of produced
		/// graph.
		/// </Summary>
		private static NFAState<TValue> ConvertToNFA(Node node, NFAState<TValue> parent)
		{
			NFAState<TValue> next;

			switch (node.Type)
			{
				case NodeType.Alternative:
					//           /-- [child1] --\
					// [parent] ---- [child2] ---> [next]
					//           \-- [child3] --/

					next = new NFAState<TValue>();

					foreach (var child in node.Children)
						ConvertToNFA(child, parent).EpsilonTo(next);

					break;

				case NodeType.Character:
					// [parent] --{begin, end}--> [next]

					next = new NFAState<TValue>();

					foreach (var range in node.Ranges)
						parent.ConnectTo(range.Begin, range.End, next);

					break;

				case NodeType.Repeat:
					//                           /-- [child] --\ * (max - min)
					// [parent] - [child] * min ----------------> [next]
					//                           \-- [child] --/ * infinite

					// Convert NFA until lower bound is reached
					for (var i = 0; i < node.RepeatMin; ++i)
						parent = ConvertToNFA(node.Children[0], parent);

					next = new NFAState<TValue>();

					parent.EpsilonTo(next);

					// Bounded repeat sequence, perform conversion (max - min) times
					if (node.RepeatMax > 0)
					{
						for (var i = 0; i < node.RepeatMax - node.RepeatMin; ++i)
						{
							parent = ConvertToNFA(node.Children[0], parent);
							parent.EpsilonTo(next);
						}
					}

					// Unbounded repeat sequence, loop converted NFA over itself
					else
					{
						var loop = ConvertToNFA(node.Children[0], parent);

						loop.EpsilonTo(parent);
						loop.EpsilonTo(next);
					}

					return next;

				case NodeType.Sequence:
					// [parent] -> [child1] -> [child2] -> ... -> [next]

					next = parent;

					foreach (var child in node.Children)
						next = ConvertToNFA(child, next);

					break;

				default:
					throw new InvalidOperationException();
			}

			return next;
		}
	}
}