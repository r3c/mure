using System.Collections.Generic;
using System.IO;
using Mure.Automata;
using Mure.Compilers.Regular;
using Mure.Matchers;

namespace Mure.Compilers
{
	internal abstract class RegularCompiler<TValue> : ICompiler<IEnumerable<(string, TValue)>, TValue>
	{
		private readonly IMatcher<Lexem> _matcher;

		protected RegularCompiler(IMatcher<Lexem> matcher)
		{
			_matcher = matcher;
		}

		public IMatcher<TValue> Compile(IEnumerable<(string, TValue)> input)
		{
			var automata = new NonDeterministicAutomata<TValue>();
			var start = automata.PushEmpty();

			foreach (var search in input)
				CompilePattern(automata, start, search.Item1, search.Item2);

			return new AutomataMatcher<TValue>(start.ToDeterministic());
		}

		protected abstract Node ParsePattern(IMatchIterator<Lexem> iterator);

		/// <Summary>
		/// Compile regular pattern into graph of non-deterministic states leading to given value.
		/// </Summary>
		private void CompilePattern(NonDeterministicAutomata<TValue> automata, NonDeterministicNode<TValue> start, string pattern, TValue value)
		{
			using (var reader = new StringReader(pattern))
			{
				var iterator = _matcher.Open(reader);
				var node = ParsePattern(iterator);
				var stop = node.ConnectTo(automata, start);

				stop.EpsilonTo(automata.PushValue(value));
			}
		}
	}
}
