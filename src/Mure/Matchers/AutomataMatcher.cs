using System.IO;
using Mure.MatchIterators;
using Mure.MatchIterators.Automata;

namespace Mure.Matchers
{
	class AutomataMatcher<TValue> : IMatcher<TValue>
	{
		private readonly DeterministicState<TValue> _start;

		public AutomataMatcher(DeterministicState<TValue> start)
		{
			_start = start;
		}

		public IMatchIterator<TValue> Open(TextReader reader)
		{
			return new AutomataMatchIterator<TValue>(_start, reader);
		}
	}
}
