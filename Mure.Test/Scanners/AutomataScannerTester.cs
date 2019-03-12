using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mure.Matchers.Automata;
using Mure.Scanners;
using NUnit.Framework;

namespace Mure.Test.Scanners
{
	public class AutomataScannerTester
	{
		[TestCase("a", false, default, default)]
		[TestCase("aa", false, default, default)]
		[TestCase("aaa", false, default, default)]
		[TestCase("aab", true, "aab", 17)]
		[TestCase("aaab", true, "aaab", 17)]
		[TestCase("ab", true, "ab", 17)]
		[TestCase("abb", true, "ab", 17)]
		[TestCase("abbb", true, "ab", 17)]
		[TestCase("b", false, default, default)]
		[TestCase("c", false, default, default)]
		public void ConnectToRange(string pattern, bool success, string expectedCapture, int expectedValue)
		{
			var q0 = new NFAState<int>();
			var q1 = new NFAState<int>();
			var q2 = new NFAState<int>(17);

			q0.ConnectTo('a', 'b', q0);
			q0.ConnectTo('a', 'a', q1);
			q1.ConnectTo('b', 'b', q2);

			ConvertAndMatch(q0, pattern, success, expectedCapture, expectedValue);
		}

		[TestCase("a", false, default, default)]
		[TestCase("aae", true, "aae", 17)]
		[TestCase("aab", false, default, default)]
		[TestCase("aabbe", true, "aabbe", 17)]
		[TestCase("aabce", true, "aabce", 17)]
		[TestCase("aabcce", false, default, default)]
		[TestCase("aabbf", true, "aabbf", 42)]
		[TestCase("aabe", true, "aabe", 17)]
		[TestCase("aabf", true, "aabf", 42)]
		[TestCase("ab", false, default, default)]
		[TestCase("abae", true, "abae", 17)]
		[TestCase("abaae", true, "abaae", 17)]
		[TestCase("abbae", true, "abbae", 17)]
		[TestCase("ae", true, "ae", 17)]
		[TestCase("af", false, default, default)]
		[TestCase("bf", true, "bf", 42)]
		[TestCase("bbf", true, "bbf", 42)]
		[TestCase("bcf", true, "bcf", 42)]
		[TestCase("bdf", true, "bdf", 42)]
		[TestCase("cf", true, "cf", 42)]
		[TestCase("df", true, "df", 42)]
		public void ConnectToOverlaps(string pattern, bool success, string expectedCapture, int expectedValue)
		{
			var q0 = new NFAState<int>();
			var q1 = new NFAState<int>();
			var q2 = new NFAState<int>();
			var q3 = new NFAState<int>(17);
			var q4 = new NFAState<int>(42);

			q0.ConnectTo('a', 'b', q0);
			q0.ConnectTo('a', 'c', q1);
			q0.ConnectTo('b', 'd', q2);
			q1.ConnectTo('e', 'e', q3);
			q2.ConnectTo('f', 'f', q4);

			ConvertAndMatch(q0, pattern, success, expectedCapture, expectedValue);
		}

		[TestCase("a", false, default, default)]
		[TestCase("aa", false, default, default)]
		[TestCase("aab", true, "aab", 17)]
		[TestCase("ab", true, "ab", 17)]
		[TestCase("b", true, "b", 17)]
		[TestCase("ba", true, "b", 17)]
		[TestCase("bb", true, "b", 17)]
		public void EpsilonTo(string pattern, bool success, string expectedCapture, int expectedValue)
		{
			var q0 = new NFAState<int>();
			var q1 = new NFAState<int>();
			var q2 = new NFAState<int>(17);

			q0.EpsilonTo(q1);
			q0.ConnectTo('a', 'a', q0);
			q1.ConnectTo('b', 'b', q2);

			ConvertAndMatch(q0, pattern, success, expectedCapture, expectedValue);
		}

		[Test]
		public void EpsilonToValue()
		{
			var q0 = new NFAState<int>();
			var q1 = new NFAState<int>();
			var q2 = new NFAState<int>(22);

			q0.ConnectTo('a', 'a', q1);
			q1.EpsilonTo(q2);

			ConvertAndMatch(q0, "a", true, "a", 22);
		}

		private static void ConvertAndMatch<TValue>(NFAState<TValue> start, string pattern, bool success, string expectedCapture, TValue? expectedValue) where TValue : struct
		{
			var scanner = new AutomataScanner<TValue>(start.ConvertToDFA());

			using (var reader = new StringReader(pattern))
			{
				var matcher = scanner.Scan(reader);

				Assert.That(matcher.TryMatch(out var match), Is.EqualTo(success));
				Assert.That(match.Capture, Is.EqualTo(expectedCapture));
				Assert.That(match.Value, Is.EqualTo(expectedValue));
			}
		}
	}
}