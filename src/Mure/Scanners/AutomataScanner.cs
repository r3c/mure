using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Mure.Matchers;
using Mure.Matchers.Automata;

namespace Mure.Scanners
{
	class AutomataScanner<TValue> : IScanner<TValue>
	{
		private readonly DFAState<TValue> _start;

		public AutomataScanner(DFAState<TValue> start)
		{
			_start = start;
		}

		public IMatcher<TValue> Scan(TextReader reader)
		{
			return new AutomataMatcher<TValue>(_start, reader);
		}
	}
}
