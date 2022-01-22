using System.IO;
using Mure.Matchers;
using Mure.MatchIterators.Automata;
using NUnit.Framework;

namespace Mure.Test.Scanners
{
	public class AutomataMatcherTester
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
			var automata = new NonDeterministicAutomata<int>();
			var q0 = automata.PushEmptyState();
			var q1 = automata.PushEmptyState();
			var q2 = automata.PushValueState(17);

			automata.BranchTo(q0, 'a', 'b', q0);
			automata.BranchTo(q0, 'a', 'a', q1);
			automata.BranchTo(q1, 'b', 'b', q2);

			ConvertAndMatch(automata, q0, pattern, success, expectedCapture, expectedValue);
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
			var automata = new NonDeterministicAutomata<int>();
			var q0 = automata.PushEmptyState();
			var q1 = automata.PushEmptyState();
			var q2 = automata.PushEmptyState();
			var q3 = automata.PushValueState(17);
			var q4 = automata.PushValueState(42);

			automata.BranchTo(q0, 'a', 'b', q0);
			automata.BranchTo(q0, 'a', 'c', q1);
			automata.BranchTo(q0, 'b', 'd', q2);
			automata.BranchTo(q1, 'e', 'e', q3);
			automata.BranchTo(q2, 'f', 'f', q4);

			ConvertAndMatch(automata, q0, pattern, success, expectedCapture, expectedValue);
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
			var automata = new NonDeterministicAutomata<int>();
			var q0 = automata.PushEmptyState();
			var q1 = automata.PushEmptyState();
			var q2 = automata.PushValueState(17);

			automata.EpsilonTo(q0, q1);
			automata.BranchTo(q0, 'a', 'a', q0);
			automata.BranchTo(q1, 'b', 'b', q2);

			ConvertAndMatch(automata, q0, pattern, success, expectedCapture, expectedValue);
		}

		[Test]
		public void EpsilonToValue()
		{
			var automata = new NonDeterministicAutomata<int>();
			var q0 = automata.PushEmptyState();
			var q1 = automata.PushEmptyState();
			var q2 = automata.PushValueState(22);

			automata.BranchTo(q0, 'a', 'a', q1);
			automata.EpsilonTo(q1, q2);

			ConvertAndMatch(automata, q0, "a", true, "a", 22);
		}

		private static void ConvertAndMatch<TValue>(NonDeterministicAutomata<TValue> automata, int start, string pattern, bool success, string expectedCapture, TValue? expectedValue) where TValue : struct
		{
			var scanner = new AutomataMatcher<TValue>(automata.ConvertToDeterministic(start));

			using (var reader = new StringReader(pattern))
			{
				var matcher = scanner.Open(reader);

				Assert.That(matcher.TryMatchNext(out var match), Is.EqualTo(success));
				Assert.That(match.Capture, Is.EqualTo(expectedCapture));
				Assert.That(match.Value, Is.EqualTo(expectedValue));
			}
		}
	}
}
