using System.Collections.Generic;
using System.IO;
using System.Text;
using Mure.MatchIterators.Automata;

namespace Mure.MatchIterators
{
	class AutomataMatchIterator<TValue> : IMatchIterator<TValue>
	{
		public int Position { get; private set; } = 0;

		private readonly List<int> _buffer;
		private readonly TextReader _reader;
		private readonly DeterministicState<TValue> _start;

		public AutomataMatchIterator(DeterministicState<TValue> start, TextReader reader)
		{
			_buffer = new List<int>();
			_reader = reader;
			_start = start;

			Position = 0;
		}

		public bool TryMatchNext(out Match<TValue> match)
		{
			var bestLength = 0;
			var bestValue = default(TValue);
			var builder = new StringBuilder();
			var current = _start;
			var index = 0;

			while (true)
			{
				// Read new character and append to buffer when reaching the end of it
				if (index >= _buffer.Count)
				{
					_buffer.Add(_reader.Read());

					++Position;
				}

				// Valid transition exists when following character from current state
				if (current.TryFollow(_buffer[index], out current))
				{
					if (current.HasValue)
					{
						bestLength = index + 1;
						bestValue = current.Value;
					}

					builder.Append((char)_buffer[index]);
				}

				// No valid transition found but we had a match
				else if (bestLength > 0)
				{
					builder.Length = bestLength;

					_buffer.RemoveRange(0, bestLength); // FIXME: slow

					match = new Match<TValue>(bestValue, builder.ToString());

					return true;
				}

				// No valid transition found but zero match is valid
				else if (_start.HasValue)
				{
					match = new Match<TValue>(_start.Value, string.Empty);

					return true;
				}

				// No valid transition found and no match either
				else
				{
					match = default;

					return false;
				}

				++index;
			}
		}
	}
}
