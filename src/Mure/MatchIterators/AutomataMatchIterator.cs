using System.Collections.Generic;
using System.IO;
using System.Text;
using Mure.Automata;

namespace Mure.MatchIterators
{
	class AutomataMatchIterator<TValue> : IMatchIterator<TValue>
	{
		public int Position => _offset - _buffer.Count;

		private readonly DeterministicAutomata<TValue> _automata;
		private readonly List<int> _buffer;
		private readonly TextReader _reader;

		private int _offset;

		public AutomataMatchIterator(DeterministicAutomata<TValue> automata, TextReader reader)
		{
			_automata = automata;
			_buffer = new List<int>();
			_offset = 0;
			_reader = reader;
		}

		public bool TryMatchNext(out Match<TValue> match)
		{
			var bestLength = 0;
			var bestValue = default(TValue);
			var builder = new StringBuilder();
			var current = _automata.Start;
			var index = 0;

			while (true)
			{
				// Read new character and append to buffer when reaching the end of it
				if (index >= _buffer.Count)
				{
					_buffer.Add(_reader.Read());

					++_offset;
				}

				// Valid transition exists when following character from current state
				if (_automata.TryFollow(current, _buffer[index], out current))
				{
					if (_automata.TryGetValue(current, out var value))
					{
						bestLength = index + 1;
						bestValue = value;
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
				else if (_automata.TryGetValue(_automata.Start, out var value))
				{
					match = new Match<TValue>(value, string.Empty);

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
