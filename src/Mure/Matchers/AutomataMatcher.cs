using System.IO;
using Mure.MatchIterators;
using Mure.MatchIterators.Automata;

namespace Mure.Matchers
{
	class AutomataMatcher<TValue> : IMatcher<TValue>
	{
		private readonly DeterministicAutomata<TValue> _automata;

		private readonly int _start;

		public AutomataMatcher((DeterministicAutomata<TValue>, int) value)
		{
			_automata = value.Item1;
			_start = value.Item2;
		}

		public IMatchIterator<TValue> Open(TextReader reader)
		{
			return new AutomataMatchIterator<TValue>(_automata, _start, reader);
		}
	}
}
