using System;
using System.Collections.Generic;
using Mure.Automata;

namespace Mure.Compilers.Pattern;

internal readonly struct Node
{
	private readonly IReadOnlyList<Node> _children;
	private readonly IReadOnlyList<NodeRange> _ranges;
	private readonly int _repeatMax;
	private readonly int _repeatMin;
	private readonly NodeType _type;

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
		_children = children;
		_ranges = ranges;
		_repeatMax = repeatMax;
		_repeatMin = repeatMin;
		_type = type;
	}

	/// <Summary>
	/// Convert compiled regular expression node into graph of non-deterministic
	/// states connected to given parent state and return final state of
	/// produced graph.
	/// </Summary>
	public int ConnectTo<TValue>(NonDeterministicAutomata<TValue> automata, int parent)
	{
		int next;

		switch (_type)
		{
			case NodeType.Alternative:
				//           /-- [child1] --\
				// [parent] ---- [child2] ---> [next]
				//           \-- [child3] --/

				next = automata.PushEmpty();

				foreach (var child in _children)
				{
					var branch = child.ConnectTo(automata, parent);

					automata.EpsilonTo(branch, next);
				}

				break;

			case NodeType.Character:
				// [parent] --{begin, end}--> [next]

				next = automata.PushEmpty();

				foreach (var range in _ranges)
					automata.BranchTo(parent, range.Begin, range.End, next);

				break;

			case NodeType.Repeat:
				//                           /-- [child] --\ * (max - min)
				// [parent] - [child] * min ----------------> [next]
				//                           \-- [child] --/ * infinite

				// Convert until lower bound is reached
				for (var i = 0; i < _repeatMin; ++i)
					parent = _children[0].ConnectTo(automata, parent);

				next = automata.PushEmpty();

				automata.EpsilonTo(parent, next);

				// Bounded repeat sequence, perform conversion (max - min) times
				if (_repeatMax >= 0)
				{
					for (var i = 0; i < _repeatMax - _repeatMin; ++i)
					{
						parent = _children[0].ConnectTo(automata, parent);
						automata.EpsilonTo(parent, next);
					}
				}

				// Unbounded repeat sequence, loop converted state over itself
				else
				{
					var loop = _children[0].ConnectTo(automata, parent);

					automata.EpsilonTo(loop, parent);
					automata.EpsilonTo(loop, next);
				}

				return next;

			case NodeType.Sequence:
				// [parent] -> [child1] -> [child2] -> ... -> [next]

				next = parent;

				foreach (var child in _children)
					next = child.ConnectTo(automata, next);

				break;

			default:
				throw new InvalidOperationException();
		}

		return next;
	}
}