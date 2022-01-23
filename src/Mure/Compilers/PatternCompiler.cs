using System.IO;
using Mure.Automata;
using Mure.Compilers.Pattern;
using Mure.Matchers;

namespace Mure.Compilers
{
	internal abstract class PatternCompiler<TValue> : ICompiler<string, TValue>
	{
		private readonly NonDeterministicAutomata<TValue> _automata;
		private readonly IMatcher<Lexem> _patternLexer;
		private readonly NonDeterministicNode<TValue> _start;

		protected PatternCompiler(IMatcher<Lexem> patternLexer)
		{
			var automata = new NonDeterministicAutomata<TValue>();

			_automata = automata;
			_patternLexer = patternLexer;
			_start = automata.PushEmpty();
		}

		public ICompiler<string, TValue> Associate(string pattern, TValue value)
		{
			AppendPattern(_automata, _start, pattern, value);

			return this;
		}

		public IMatcher<TValue> Compile()
		{
			return new AutomataMatcher<TValue>(_start.ToDeterministic());
		}

		protected abstract Node CreateGraph(IMatchIterator<Lexem> iterator);

		/// <Summary>
		/// Compile regular pattern into graph of non-deterministic states leading to given value.
		/// </Summary>
		private void AppendPattern(NonDeterministicAutomata<TValue> automata, NonDeterministicNode<TValue> start, string pattern, TValue value)
		{
			var node = ParsePattern(pattern);
			var stop = node.ConnectTo(automata, start);
			var tail = automata.PushValue(value);

			stop.EpsilonTo(tail);
		}

		private Node ParsePattern(string pattern)
		{
			using var reader = new StringReader(pattern);

			var iterator = _patternLexer.Open(reader);

			return CreateGraph(iterator);
		}
	}
}
