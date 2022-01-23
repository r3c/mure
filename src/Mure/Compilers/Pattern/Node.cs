using System;
using System.Collections.Generic;
using Mure.Automata;

namespace Mure.Compilers.Pattern
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
		public NonDeterministicNode<TValue> ConnectTo<TValue>(NonDeterministicAutomata<TValue> automata, NonDeterministicNode<TValue> parent)
		{
			NonDeterministicNode<TValue> next;

			switch (Type)
			{
				case NodeType.Alternative:
					//           /-- [child1] --\
					// [parent] ---- [child2] ---> [next]
					//           \-- [child3] --/

					next = automata.PushEmpty();

					foreach (var child in Children)
						child.ConnectTo(automata, parent).EpsilonTo(next);

					break;

				case NodeType.Character:
					// [parent] --{begin, end}--> [next]

					next = automata.PushEmpty();

					foreach (var range in Ranges)
						parent.BranchTo(range.Begin, range.End, next);

					break;

				case NodeType.Repeat:
					//                           /-- [child] --\ * (max - min)
					// [parent] - [child] * min ----------------> [next]
					//                           \-- [child] --/ * infinite

					// Convert until lower bound is reached
					for (var i = 0; i < RepeatMin; ++i)
						parent = Children[0].ConnectTo(automata, parent);

					next = automata.PushEmpty();

					parent.EpsilonTo(next);

					// Bounded repeat sequence, perform conversion (max - min) times
					if (RepeatMax >= 0)
					{
						for (var i = 0; i < RepeatMax - RepeatMin; ++i)
						{
							parent = Children[0].ConnectTo(automata, parent);
							parent.EpsilonTo(next);
						}
					}

					// Unbounded repeat sequence, loop converted state over itself
					else
					{
						var loop = Children[0].ConnectTo(automata, parent);

						loop.EpsilonTo(parent);
						loop.EpsilonTo(next);
					}

					return next;

				case NodeType.Sequence:
					// [parent] -> [child1] -> [child2] -> ... -> [next]

					next = parent;

					foreach (var child in Children)
						next = child.ConnectTo(automata, next);

					break;

				default:
					throw new InvalidOperationException();
			}

			return next;
		}
	}
}
