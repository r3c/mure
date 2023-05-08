using System.IO;
using Mure.Automata;
using Mure.MatchIterators;

namespace Mure.Matchers;

internal class AutomataMatcher<TValue> : IMatcher<TValue>
{
	private readonly DeterministicAutomata<TValue> _automata;

	public AutomataMatcher(DeterministicAutomata<TValue> automata)
	{
		_automata = automata;
	}

	public IMatchIterator<TValue> Open(TextReader reader)
	{
		return new AutomataMatchIterator<TValue>(_automata, reader);
	}
}