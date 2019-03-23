using System;
using System.IO;
using Mure.Compilers;
using NUnit.Framework;

namespace Mure.Test.Compilers
{
	class RegexCompilerTester
	{
		[TestCase("a|b", "", null)]
		[TestCase("a|b", "a", "a")]
		[TestCase("a|b", "b", "b")]
		[TestCase("a|b", "ab", "a")]
		[TestCase("a|b", "ba", "b")]
		[TestCase("a|b", "c", null)]
		public void Alternate(string pattern, string subject, string capture)
		{
			CompileAndAssert(pattern, subject, capture);
		}

		[TestCase("[]]", "", null)]
		[TestCase("[]]", "]", "]")]

		[TestCase("[a]", "", null)]
		[TestCase("[a]", "a", "a")]
		[TestCase("[a]", "b", null)]

		[TestCase("[ab]", "", null)]
		[TestCase("[ab]", "a", "a")]
		[TestCase("[ab]", "b", "b")]
		[TestCase("[ab]", "c", null)]

		[TestCase("[a-c]", "", null)]
		[TestCase("[a-c]", "a", "a")]
		[TestCase("[a-c]", "b", "b")]
		[TestCase("[a-c]", "c", "c")]
		[TestCase("[a-c]", "d", null)]

		[TestCase("[ab]ab", "a", null)]
		[TestCase("[ab]ab", "aa", null)]
		[TestCase("[ab]ab", "aaa", null)]
		[TestCase("[ab]ab", "aaaa", null)]
		[TestCase("[ab]ab", "aaab", null)]
		[TestCase("[ab]ab", "aab", "aab")]
		[TestCase("[ab]ab", "aaba", "aab")]
		[TestCase("[ab]ab", "aabb", "aab")]
		[TestCase("[ab]ab", "ab", null)]
		[TestCase("[ab]ab", "aba", null)]
		[TestCase("[ab]ab", "abaa", null)]
		[TestCase("[ab]ab", "abab", null)]
		[TestCase("[ab]ab", "abbb", null)]
		[TestCase("[ab]ab", "b", null)]
		[TestCase("[ab]ab", "ba", null)]
		[TestCase("[ab]ab", "baa", null)]
		[TestCase("[ab]ab", "baaa", null)]
		[TestCase("[ab]ab", "baab", null)]
		[TestCase("[ab]ab", "bab", "bab")]
		[TestCase("[ab]ab", "baba", "bab")]
		[TestCase("[ab]ab", "babb", "bab")]
		[TestCase("[ab]ab", "bb", null)]
		[TestCase("[ab]ab", "bba", null)]
		[TestCase("[ab]ab", "bbaa", null)]
		[TestCase("[ab]ab", "bbab", null)]
		[TestCase("[ab]ab", "bbba", null)]
		[TestCase("[ab]ab", "bbbb", null)]
		public void Class(string pattern, string subject, string capture)
		{
			CompileAndAssert(pattern, subject, capture);
		}

		[TestCase("[a")]

		[TestCase("a{1")]
		[TestCase("a{a")]
		[TestCase("a{2,1}")]
		[TestCase("a{1,1,1}")]

		[TestCase("\\i")]
		public void DetectSyntaxError(string pattern)
		{
			Assert.Throws<ArgumentException>(() => ScannerFactory.CreateRegex(new[]
			{
				(pattern, true)
			}));
		}

		[TestCase("ab", "a", null)]
		[TestCase("ab", "aa", null)]
		[TestCase("ab", "ab", "ab")]
		[TestCase("ab", "aba", "ab")]
		[TestCase("ab", "abb", "ab")]
		[TestCase("ab", "bb", null)]
		public void Literal(string pattern, string subject, string capture)
		{
			CompileAndAssert(pattern, subject, capture);
		}

		[TestCase("a+", "", null)]
		[TestCase("a+", "a", "a")]
		[TestCase("a+", "aa", "aa")]
		[TestCase("a+", "aaaaa", "aaaaa")]
		[TestCase("a+", "aaab", "aaa")]
		[TestCase("a+", "b", null)]
		public void OneOrMore(string pattern, string subject, string capture)
		{
			CompileAndAssert(pattern, subject, capture);
		}

		[TestCase("a{1}", "", null)]
		[TestCase("a{1}", "a", "a")]
		[TestCase("a{1}", "aa", "a")]
		[TestCase("a{3}", "aa", null)]
		[TestCase("a{3}", "aaa", "aaa")]
		[TestCase("a{3}", "aaaa", "aaa")]
		[TestCase("a{1,2}", "", null)]
		[TestCase("a{1,2}", "a", "a")]
		[TestCase("a{1,2}", "aa", "aa")]
		[TestCase("a{1,2}", "aaa", "aa")]
		[TestCase("a{,1}", "", "")]
		[TestCase("a{,1}", "a", "a")]
		[TestCase("a{,1}", "aa", "a")]
		[TestCase("a{1,}", "", null)]
		[TestCase("a{1,}", "a", "a")]
		[TestCase("a{1,}", "aa", "aa")]
		public void Repeat(string pattern, string subject, string capture)
		{
			CompileAndAssert(pattern, subject, capture);
		}

		[TestCase("()", "", "")]
		[TestCase("(a)", "a", "a")]
		[TestCase("a(b)c", "abc", "abc")]
		[TestCase("a(bc)d", "abcd", "abcd")]
		[TestCase("a(bc)?d", "ad", "ad")]
		[TestCase("a(bc)?d", "abcd", "abcd")]
		[TestCase("a(bc)?d", "abcbcd", null)]
		[TestCase("a(bc)+d", "ad", null)]
		[TestCase("a(bc)+d", "abcd", "abcd")]
		[TestCase("a(bc)+d", "abcbcd", "abcbcd")]
		[TestCase("a(bc)*d", "ad", "ad")]
		[TestCase("a(bc)*d", "abcd", "abcd")]
		[TestCase("a(bc)*d", "abcbcd", "abcbcd")]
		[TestCase("a(b|c)*d", "ad", "ad")]
		[TestCase("a(b|c)*d", "abd", "abd")]
		[TestCase("a(b|c)*d", "acd", "acd")]
		[TestCase("a(b|c)*d", "abccbd", "abccbd")]
		public void Sequence(string pattern, string subject, string capture)
		{
			CompileAndAssert(pattern, subject, capture);
		}

		[TestCase("\\(", "(", "(")]
		[TestCase("\\)", ")", ")")]
		[TestCase("\\*", "*", "*")]
		[TestCase("\\+", "+", "+")]
		[TestCase("\\-", "-", "-")]
		[TestCase("\\.", ".", ".")]
		[TestCase("\\?", "?", "?")]
		[TestCase("\\[", "[", "[")]
		[TestCase("\\]", "]", "]")]
		[TestCase("\\\\'", "\\'", "\\'")]
		[TestCase("\\^", "^", "^")]
		[TestCase("\\{", "{", "{")]
		[TestCase("\\|", "|", "|")]
		[TestCase("\\}", "}", "}")]
		[TestCase("\\n", "\n", "\n")]
		[TestCase("\\r", "\r", "\r")]
		[TestCase("\\t", "\t", "\t")]
		public void Special(string pattern, string subject, string capture)
		{
			CompileAndAssert(pattern, subject, capture);
		}

		[TestCase(".", "a", "a")]
		[TestCase(".", "b", "b")]
		public void Wildcard(string pattern, string subject, string capture)
		{
			CompileAndAssert(pattern, subject, capture);
		}

		[TestCase("a*", "", "")]
		[TestCase("a*", "a", "a")]
		[TestCase("a*", "aa", "aa")]
		[TestCase("a*", "aaaaa", "aaaaa")]
		[TestCase("a*", "aaab", "aaa")]
		[TestCase("a*", "b", "")]
		public void ZeroOrMore(string pattern, string subject, string capture)
		{
			CompileAndAssert(pattern, subject, capture);
		}

		[TestCase("a?", "", "")]
		[TestCase("a?", "a", "a")]
		[TestCase("a?", "aa", "a")]
		[TestCase("a?", "b", "")]
		public void ZeroOrOne(string pattern, string subject, string capture)
		{
			CompileAndAssert(pattern, subject, capture);
		}

		private static void CompileAndAssert(string pattern, string subject, string capture)
		{
			var scanner = ScannerFactory.CreateRegex(new[]
			{
				(pattern, true)
			});

			using (var reader = new StringReader(subject))
			{
				var expected = capture != null;
				var matcher = scanner.Scan(reader);

				Assert.That(matcher.TryMatch(out var match), Is.EqualTo(expected));
				Assert.That(match.Capture, Is.EqualTo(capture));
				Assert.That(match.Value, Is.EqualTo(expected));
			}
		}
	}
}