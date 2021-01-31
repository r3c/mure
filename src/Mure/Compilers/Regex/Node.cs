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

		public static Node CreateAlternative(IReadOnlyList<IReadOnlyList<Node>> children)
		{
			var nodes = new Node[children.Count];

			for (var i = 0; i < children.Count; ++i)
			{
				nodes[i] = children[i].Count == 1
					? children[i][0]
					: new Node(NodeType.Sequence, children[i], Array.Empty<NodeRange>(), 0, 0);
			}

			if (nodes.Length == 1)
				return nodes[0];

			return new Node(NodeType.Alternative, nodes, Array.Empty<NodeRange>(), 0, 0);
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
			return new Node(NodeType.Repeat, new[] { child }, Array.Empty<NodeRange>(), min, max);
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
		public NonDeterministicState<TValue> ConvertToState<TValue>(NonDeterministicState<TValue> parent)
		{
			NonDeterministicState<TValue> next;

			switch (Type)
			{
				case NodeType.Alternative:
					//           /-- [child1] --\
					// [parent] ---- [child2] ---> [next]
					//           \-- [child3] --/

					next = new NonDeterministicState<TValue>();

					foreach (var child in Children)
						child.ConvertToState(parent).EpsilonTo(next);

					break;

				case NodeType.Character:
					// [parent] --{begin, end}--> [next]

					next = new NonDeterministicState<TValue>();

					foreach (var range in Ranges)
						parent.ConnectTo(range.Begin, range.End, next);

					break;

				case NodeType.Repeat:
					//                           /-- [child] --\ * (max - min)
					// [parent] - [child] * min ----------------> [next]
					//                           \-- [child] --/ * infinite

					// Convert until lower bound is reached
					for (var i = 0; i < RepeatMin; ++i)
						parent = Children[0].ConvertToState(parent);

					next = new NonDeterministicState<TValue>();

					parent.EpsilonTo(next);

					// Bounded repeat sequence, perform conversion (max - min) times
					if (RepeatMax > 0)
					{
						for (var i = 0; i < RepeatMax - RepeatMin; ++i)
						{
							parent = Children[0].ConvertToState(parent);
							parent.EpsilonTo(next);
						}
					}

					// Unbounded repeat sequence, loop converted state over itself
					else
					{
						var loop = Children[0].ConvertToState(parent);

						loop.EpsilonTo(parent);
						loop.EpsilonTo(next);
					}

					return next;

				case NodeType.Sequence:
					// [parent] -> [child1] -> [child2] -> ... -> [next]

					next = parent;

					foreach (var child in Children)
						next = child.ConvertToState(next);

					break;

				default:
					throw new InvalidOperationException();
			}

			return next;
		}
	}
}
