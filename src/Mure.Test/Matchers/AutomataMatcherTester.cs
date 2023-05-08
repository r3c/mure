using System.IO;
using Mure.Automata;
using Mure.Matchers;
using NUnit.Framework;

namespace Mure.Test.Matchers
{
	internal class AutomataMatcherTester
	{
		[TestCase("a", false, null, 0)]
		[TestCase("aa", false, null, 0)]
		[TestCase("aaa", false, null, 0)]
		[TestCase("aab", true, "aab", 17)]
		[TestCase("aaab", true, "aaab", 17)]
		[TestCase("ab", true, "ab", 17)]
		[TestCase("abb", true, "ab", 17)]
		[TestCase("abbb", true, "ab", 17)]
		[TestCase("b", false, null, 0)]
		[TestCase("c", false, null, 0)]
		public void ConnectToRange(string pattern, bool success, string expectedCapture, int expectedValue)
		{
			var automata = new NonDeterministicAutomata<int>();
			var q0 = automata.PushEmpty();
			var q1 = automata.PushEmpty();
			var q2 = automata.PushValue(17);

			q0.BranchTo('a', 'b', q0);
			q0.BranchTo('a', 'a', q1);
			q1.BranchTo('b', 'b', q2);

			ConvertAndMatch(q0, pattern, success, expectedCapture, expectedValue);
		}

		[TestCase("a", false, null, 0)]
		[TestCase("aae", true, "aae", 17)]
		[TestCase("aab", false, null, 0)]
		[TestCase("aabbe", true, "aabbe", 17)]
		[TestCase("aabce", true, "aabce", 17)]
		[TestCase("aabcce", false, null, 0)]
		[TestCase("aabbf", true, "aabbf", 42)]
		[TestCase("aabe", true, "aabe", 17)]
		[TestCase("aabf", true, "aabf", 42)]
		[TestCase("ab", false, null, 0)]
		[TestCase("abae", true, "abae", 17)]
		[TestCase("abaae", true, "abaae", 17)]
		[TestCase("abbae", true, "abbae", 17)]
		[TestCase("ae", true, "ae", 17)]
		[TestCase("af", false, null, 0)]
		[TestCase("bf", true, "bf", 42)]
		[TestCase("bbf", true, "bbf", 42)]
		[TestCase("bcf", true, "bcf", 42)]
		[TestCase("bdf", true, "bdf", 42)]
		[TestCase("cf", true, "cf", 42)]
		[TestCase("df", true, "df", 42)]
		public void ConnectToOverlaps(string pattern, bool success, string? expectedCapture, int expectedValue)
		{
			var automata = new NonDeterministicAutomata<int>();
			var q0 = automata.PushEmpty();
			var q1 = automata.PushEmpty();
			var q2 = automata.PushEmpty();
			var q3 = automata.PushValue(17);
			var q4 = automata.PushValue(42);

			q0.BranchTo('a', 'b', q0);
			q0.BranchTo('a', 'c', q1);
			q0.BranchTo('b', 'd', q2);
			q1.BranchTo('e', 'e', q3);
			q2.BranchTo('f', 'f', q4);

			ConvertAndMatch(q0, pattern, success, expectedCapture, expectedValue);
		}

		[TestCase("a", false, null, 0)]
		[TestCase("aa", false, null, 0)]
		[TestCase("aab", true, "aab", 17)]
		[TestCase("ab", true, "ab", 17)]
		[TestCase("b", true, "b", 17)]
		[TestCase("ba", true, "b", 17)]
		[TestCase("bb", true, "b", 17)]
		public void EpsilonTo(string pattern, bool success, string? expectedCapture, int expectedValue)
		{
			var automata = new NonDeterministicAutomata<int>();
			var q0 = automata.PushEmpty();
			var q1 = automata.PushEmpty();
			var q2 = automata.PushValue(17);

			q0.EpsilonTo(q1);
			q0.BranchTo('a', 'a', q0);
			q1.BranchTo('b', 'b', q2);

			ConvertAndMatch(q0, pattern, success, expectedCapture, expectedValue);
		}

		[Test]
		public void EpsilonToValue()
		{
			var automata = new NonDeterministicAutomata<int>();
			var q0 = automata.PushEmpty();
			var q1 = automata.PushEmpty();
			var q2 = automata.PushValue(22);

			q0.BranchTo('a', 'a', q1);
			q1.EpsilonTo(q2);

			ConvertAndMatch(q0, "a", true, "a", 22);
		}

		private static void ConvertAndMatch<TValue>(NonDeterministicNode<TValue> node, string pattern, bool success, string? expectedCapture, TValue? expectedValue) where TValue : struct
		{
			using var reader = new StringReader(pattern);

			var automata = node.ToDeterministic();

			Assert.That(automata.Error, Is.EqualTo(ConversionError.None));

			var scanner = new AutomataMatcher<TValue>(automata.Result);
			var matcher = scanner.Open(reader);

			Assert.That(matcher.TryMatchNext(out var match), Is.EqualTo(success));
			Assert.That(match.Capture, Is.EqualTo(expectedCapture));
			Assert.That(match.Value, Is.EqualTo(expectedValue));
		}
	}
}
