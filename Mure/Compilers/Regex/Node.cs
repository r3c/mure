using System;
using System.Collections.Generic;

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
	}
}