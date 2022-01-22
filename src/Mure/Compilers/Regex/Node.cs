using System;
using System.Collections.Generic;
using Mure.MatchIterators.Automata;

namespace Mure.Compilers.Regex
{
	readonly struct Node
	{
		public readonly IReadOnlyList<Node> Children;
		public readonly IReadOnlyList<NodeRange> Ranges;
		public readonly int RepeatMax;
		public readonly int RepeatMin;
		public readonly NodeType Type;

		public static Node CreateAlternative(IReadOnlyList<Node> children)
		{
			return children.Count == 1 ? children[0] : new Node(NodeType.Alternative, children, Array.Empty<NodeRange>(), 0, 0);
		}

		public static Node CreateCharacter(IReadOnlyList<NodeRange> ranges)
		{
			return new Node(NodeType.Character, Array.Empty<Node>(), ranges, 0, 0);
		}

		public static Node CreateCharacter(char character)
		{
			return new Node(NodeType.Character, Array.Empty<Node>(), new[] { new NodeRange(character, character) }, 0, 0);
		}

		public static Node CreateRepeat(Node child, int min, int max)
		{
			return min == 1 && max == 1 ? child : new Node(NodeType.Repeat, new[] { child }, Array.Empty<NodeRange>(), min, max);
		}

		public static Node CreateSequence(IReadOnlyList<Node> children)
		{
			return children.Count == 1 ? children[0] : new Node(NodeType.Sequence, children, Array.Empty<NodeRange>(), 0, 0);
		}

		private Node(NodeType type, IReadOnlyList<Node> children, IReadOnlyList<NodeRange> ranges, int repeatMin, int repeatMax)
		{
			Children = children;
			Ranges = ranges;
			RepeatMax = repeatMax;
			RepeatMin = repeatMin;
			Type = type;
		}

		/// <Summary>
		/// Convert compiled regular expression node into graph of non-deterministic
		/// states connected to given parent state and return final state of
		/// produced graph.
		/// </Summary>
		public int ConvertToState<TValue>(NonDeterministicAutomata<TValue> automata, int index)
		{
			int next;

			switch (Type)
			{
				case NodeType.Alternative:
					//           /-- [child1] --\
					// [parent] ---- [child2] ---> [next]
					//           \-- [child3] --/

					next = automata.PushEmpty();

					foreach (var child in Children)
						automata.EpsilonTo(child.ConvertToState(automata, index), next);

					break;

				case NodeType.Character:
					// [parent] --{begin, end}--> [next]

					next = automata.PushEmpty();

					foreach (var range in Ranges)
						automata.BranchTo(index, range.Begin, range.End, next);

					break;

				case NodeType.Repeat:
					//                           /-- [child] --\ * (max - min)
					// [parent] - [child] * min ----------------> [next]
					//                           \-- [child] --/ * infinite

					// Convert until lower bound is reached
					for (var i = 0; i < RepeatMin; ++i)
						index = Children[0].ConvertToState(automata, index);

					next = automata.PushEmpty();

					automata.EpsilonTo(index, next);

					// Bounded repeat sequence, perform conversion (max - min) times
					if (RepeatMax >= 0)
					{
						for (var i = 0; i < RepeatMax - RepeatMin; ++i)
						{
							index = Children[0].ConvertToState(automata, index);

							automata.EpsilonTo(index, next);
						}
					}

					// Unbounded repeat sequence, loop converted state over itself
					else
					{
						var loop = Children[0].ConvertToState(automata, index);

						automata.EpsilonTo(loop, index);
						automata.EpsilonTo(loop, next);
					}

					return next;

				case NodeType.Sequence:
					// [parent] -> [child1] -> [child2] -> ... -> [next]

					next = index;

					foreach (var child in Children)
						next = child.ConvertToState(automata, next);

					break;

				default:
					throw new InvalidOperationException();
			}

			return next;
		}
	}
}
