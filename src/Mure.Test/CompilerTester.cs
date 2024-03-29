using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace Mure.Test;

internal class CompilerTester
{
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

	[TestCase("[a-c]1[d-f]2", "a1d2y", "a1d2")]
	[TestCase("[a-c]1[d-f]2", "b1e2y", "b1e2")]
	[TestCase("[a-c]1[d-f]2", "c1f2y", "c1f2")]
	[TestCase("[a-c]1[d-f]2", "a1b2", null)]
	[TestCase("[a-c]1[d-f]2", "d1e2", null)]
	public void CreateFromGlob_MatchClass(string pattern, string subject, string capture)
	{
		CompileGlobAndAssert(pattern, subject, capture);
	}

	[TestCase("\\*", "*", "*")]
	[TestCase("\\?", "?", "?")]
	[TestCase("\\[", "[", "[")]
	[TestCase("\\]", "]", "]")]
	[TestCase("\\\\'", "\\'", "\\'")]
	public void CreateFromGlob_MatchEscape(string pattern, string subject, string capture)
	{
		CompileGlobAndAssert(pattern, subject, capture);
	}

	[TestCase("", "", "")]
	[TestCase("a", "a", "a")]
	[TestCase("a", "b", null)]
	[TestCase("abc", "abc", "abc")]
	[TestCase("abc", "xabc", null)]
	public void CreateFromGlob_MatchSequence(string pattern, string subject, string capture)
	{
		CompileGlobAndAssert(pattern, subject, capture);
	}

	[TestCase("?", "", null)]
	[TestCase("?", "a", "a")]
	[TestCase("?", "b", "b")]
	public void CreateFromGlob_MatchWildcard(string pattern, string subject, string capture)
	{
		CompileGlobAndAssert(pattern, subject, capture);
	}

	[TestCase("*", "", "")]
	[TestCase("*", "a", "a")]
	[TestCase("*", "abc", "abc")]
	public void CreateFromGlob_MatchZeroOrMore(string pattern, string subject, string capture)
	{
		CompileGlobAndAssert(pattern, subject, capture);
	}

	[TestCase("", "a?")]
	[TestCase("a", "aa?")]
	[TestCase("a+", "a*")]
	[TestCase("a|b", "b|c")]
	public void CreateFromRegex_DetectPatternConflict(string pattern1, string pattern2)
	{
		var compiler = Compiler.CreateFromRegex<bool>();

		compiler.AddPattern(pattern1, false);
		compiler.AddPattern(pattern2, true);

		var exception = Assert.Throws<InvalidOperationException>(() => compiler.Compile());

		Assert.That(exception?.Message, Is.EqualTo("transition collision between multiple values: False, True"));
	}

	[TestCase("[a", "unfinished characters class at position 3")]
	[TestCase("a{1", "expected end of repeat specifier at position 4")]
	[TestCase("a{a", "expected end of repeat specifier at position 3")]
	[TestCase("a{2,1}", "invalid repeat sequence at position 6")]
	[TestCase("a{1,1,1}", "expected end of repeat specifier at position 6")]
	[TestCase("\\i", "unrecognized character at position 0")]
	public void CreateFromRegex_DetectSyntaxError(string pattern, string message)
	{
		var compiler = Compiler.CreateFromRegex<bool>();
		var exception = Assert.Throws<ArgumentException>(() => compiler.AddPattern(pattern, default));

		Assert.That(exception?.Message, Is.EqualTo(message));
	}

	[TestCase("1\t+\n1", "Integer(1),Plus,Integer(1),End")]
	[TestCase("1 + (2 - 3)", "Integer(1),Plus,ParenthesisBegin,Integer(2),Minus,Integer(3),ParenthesisEnd,End")]
	public void CreateFromRegex_IterateExpression(string expression, string expected)
	{
		var matcher = Compiler
			.CreateFromRegex<Func<string, string>?>()
			.AddEndOfFile(_ => "End")
			.AddPattern("[0-9]+", capture => $"Integer({capture})")
			.AddPattern("\\+", _ => "Plus")
			.AddPattern("-", _ => "Minus")
			.AddPattern("\\(", _ => "ParenthesisBegin")
			.AddPattern("\\)", _ => "ParenthesisEnd")
			.AddPattern("[\n\r\t ]+", null)
			.Compile();

		using var reader = new StringReader(expression);

		var values = matcher.Open(reader)
			.Where(match => match.Value is not null)
			.Select(match => match.Value!(match.Capture))
			.ToList();

		Assert.That(string.Join(",", values), Is.EqualTo(expected));
	}

	[TestCase("a|b", "", null)]
	[TestCase("a|b", "a", "a")]
	[TestCase("a|b", "b", "b")]
	[TestCase("a|b", "ab", "a")]
	[TestCase("a|b", "ba", "b")]
	[TestCase("a|b", "c", null)]
	public void CreateFromRegex_MatchAlternate(string pattern, string subject, string capture)
	{
		CompileRegexAndAssert(pattern, subject, capture);
	}

	[TestCase("[]]", "", null)]
	[TestCase("[]]", "a", null)]
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

	[TestCase("[a-c]1[d-f]2", "a1d2y", "a1d2")]
	[TestCase("[a-c]1[d-f]2", "b1e2y", "b1e2")]
	[TestCase("[a-c]1[d-f]2", "c1f2y", "c1f2")]
	[TestCase("[a-c]1[d-f]2", "a1b2", null)]
	[TestCase("[a-c]1[d-f]2", "d1e2", null)]

	[TestCase("[b-dc-e]", "a", null)]
	[TestCase("[b-dc-e]", "b", "b")]
	[TestCase("[b-dc-e]", "c", "c")]
	[TestCase("[b-dc-e]", "d", "d")]
	[TestCase("[b-dc-e]", "e", "e")]
	[TestCase("[b-dc-e]", "f", null)]

	[TestCase("[^]]", "", null)]
	[TestCase("[^]]", "a", "a")]
	[TestCase("[^]]", "]", null)]

	[TestCase("[^a]", "", null)]
	[TestCase("[^a]", "a", null)]
	[TestCase("[^a]", "b", "b")]

	[TestCase("[^b-c]", "", null)]
	[TestCase("[^b-c]", "a", "a")]
	[TestCase("[^b-c]", "b", null)]
	[TestCase("[^b-c]", "c", null)]
	[TestCase("[^b-c]", "d", "d")]

	[TestCase("[^b-dc-e]", "", null)]
	[TestCase("[^b-dc-e]", "a", "a")]
	[TestCase("[^b-dc-e]", "b", null)]
	[TestCase("[^b-dc-e]", "c", null)]
	[TestCase("[^b-dc-e]", "d", null)]
	[TestCase("[^b-dc-e]", "e", null)]
	[TestCase("[^b-dc-e]", "f", "f")]
	public void CreateFromRegex_MatchClass(string pattern, string subject, string capture)
	{
		CompileRegexAndAssert(pattern, subject, capture);
	}

	[TestCase("\\(", "(")]
	[TestCase("\\)", ")")]
	[TestCase("\\*", "*")]
	[TestCase("\\+", "+")]
	[TestCase("\\-", "-")]
	[TestCase("\\.", ".")]
	[TestCase("\\?", "?")]
	[TestCase("\\[", "[")]
	[TestCase("\\]", "]")]
	[TestCase("\\\\", "\\")]
	[TestCase("\\^", "^")]
	[TestCase("\\{", "{")]
	[TestCase("\\|", "|")]
	[TestCase("\\}", "}")]
	[TestCase("\\n", "\n")]
	[TestCase("\\r", "\r")]
	[TestCase("\\t", "\t")]
	public void CreateFromRegex_MatchEscape(string pattern, string subject)
	{
		CompileRegexAndAssert(pattern, subject, subject);
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
	[TestCase("a(b|c){1,2}d", "ad", null)]
	[TestCase("a(b|c){1,2}d", "abd", "abd")]
	[TestCase("a(b|c){1,2}d", "abcd", "abcd")]
	[TestCase("a(b|c){1,2}d", "abbbd", null)]
	[TestCase("[0-9]{0,2}(a|b){1,2}", "a", "a")]
	[TestCase("[0-9]{0,2}(a|b){1,2}", "0ba", "0ba")]
	[TestCase("[0-9]{0,2}(a|b){1,2}", "45a", "45a")]
	[TestCase("[0-9]{0,2}(a|b){1,2}", "782bb", null)]
	[TestCase("[0-9]{0,2}(a|b){1,2}", "72", null)]
	[TestCase("[^0-9]{1,2}[0-9]{1,2}", "1", null)]
	[TestCase("[^0-9]{1,2}[0-9]{1,2}", "a", null)]
	[TestCase("[^0-9]{1,2}[0-9]{1,2}", "a1", "a1")]
	[TestCase("[^0-9]{1,2}[0-9]{1,2}", "a12", "a12")]
	[TestCase("[^0-9]{1,2}[0-9]{1,2}", "ab1", "ab1")]
	[TestCase("[^0-9]{1,2}[0-9]{1,2}", "ab12", "ab12")]
	[TestCase("[^0-9]{1,2}[0-9]{1,2}", "ab123", "ab12")]
	[TestCase("[^0-9]{1,2}[0-9]{1,2}", "abc12", null)]
	[TestCase("a(b(c){3}d){2}e", "abcccdbcccde", "abcccdbcccde")]
	[TestCase("(a|b)(c|d)", "ab", null)]
	[TestCase("(a|b)(c|d)", "ac", "ac")]
	[TestCase("(a|b)(c|d)", "ad", "ad")]
	[TestCase("(a|b)(c|d)", "bc", "bc")]
	[TestCase("(a|b)(c|d)", "bd", "bd")]
	[TestCase("(a|b)(c|d)", "cd", null)]
	public void CreateFromRegex_MatchMixed(string pattern, string subject, string capture)
	{
		CompileRegexAndAssert(pattern, subject, capture);
	}

	[TestCase("a+", "", null)]
	[TestCase("a+", "a", "a")]
	[TestCase("a+", "aa", "aa")]
	[TestCase("a+", "aaaaa", "aaaaa")]
	[TestCase("a+", "aaab", "aaa")]
	[TestCase("a+", "b", null)]
	public void CreateFromRegex_MatchOneOrMore(string pattern, string subject, string capture)
	{
		CompileRegexAndAssert(pattern, subject, capture);
	}

	[TestCase("a{0}b", "b", "b")]
	[TestCase("a{0}b", "ab", null)]
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
	public void CreateFromRegex_MatchRepeat(string pattern, string subject, string capture)
	{
		CompileRegexAndAssert(pattern, subject, capture);
	}

	[TestCase("ab", "a", null)]
	[TestCase("ab", "aa", null)]
	[TestCase("ab", "ab", "ab")]
	[TestCase("ab", "aba", "ab")]
	[TestCase("ab", "abb", "ab")]
	[TestCase("ab", "bb", null)]
	public void CreateFromRegex_MatchSequence(string pattern, string subject, string capture)
	{
		CompileRegexAndAssert(pattern, subject, capture);
	}

	[TestCase(".", "a", "a")]
	[TestCase(".", "b", "b")]
	public void CreateFromRegex_MatchWildcard(string pattern, string subject, string capture)
	{
		CompileRegexAndAssert(pattern, subject, capture);
	}

	[TestCase("a*", "", "")]
	[TestCase("a*", "a", "a")]
	[TestCase("a*", "aa", "aa")]
	[TestCase("a*", "aaaaa", "aaaaa")]
	[TestCase("a*", "aaab", "aaa")]
	[TestCase("a*", "b", "")]
	public void CreateFromRegex_MatchZeroOrMore(string pattern, string subject, string capture)
	{
		CompileRegexAndAssert(pattern, subject, capture);
	}

	[TestCase("a?", "", "")]
	[TestCase("a?", "a", "a")]
	[TestCase("a?", "aa", "a")]
	[TestCase("a?", "b", "")]
	public void CreateFromRegex_MatchZeroOrOne(string pattern, string subject, string capture)
	{
		CompileRegexAndAssert(pattern, subject, capture);
	}

	private static void CompileGlobAndAssert(string pattern, string subject, string capture)
	{
		var compiler = Compiler
			.CreateFromGlob<bool>()
			.AddPattern(pattern, true);

		CompileAndAssert(compiler, subject, capture);
	}

	private static void CompileRegexAndAssert(string pattern, string subject, string capture)
	{
		var compiler = Compiler
			.CreateFromRegex<bool>()
			.AddPattern(pattern, true);

		CompileAndAssert(compiler, subject, capture);
	}

	private static void CompileAndAssert(ICompiler<string, bool> compiler, string subject, string? capture)
	{
		var expected = capture is not null;
		var matcher = compiler.Compile();

		using var reader = new StringReader(subject);

		var iterator = matcher.Open(reader);

		Assert.That(iterator.TryMatchNext(out var match), Is.EqualTo(expected));
		Assert.That(match.Capture, Is.EqualTo(capture));
		Assert.That(match.Value, Is.EqualTo(expected));
	}
}
