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

		public ICompiler<string, TValue> AddEndOfFile(TValue value)
		{
			_start.BranchTo(-1, -1, _automata.PushValue(value));

			return this;
		}

		public ICompiler<string, TValue> AddPattern(string pattern, TValue value)
		{
			var node = ParsePattern(pattern);
			var stop = node.ConnectTo(_automata, _start);
			var tail = _automata.PushValue(value);

			stop.EpsilonTo(tail);

			return this;
		}

		public IMatcher<TValue> Compile()
		{
			return new AutomataMatcher<TValue>(_start.ToDeterministic());
		}

		/// <Summary>
		/// Compile regular pattern into graph of non-deterministic states leading to given value.
		/// </Summary>
		protected abstract Node CreateGraph(IMatchIterator<Lexem> iterator);

		private Node ParsePattern(string pattern)
		{
			using var reader = new StringReader(pattern);

			var iterator = _patternLexer.Open(reader);

			return CreateGraph(iterator);
		}
	}
}
