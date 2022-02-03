using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Mure.Peg;
using Mure.Peg.Generators;
using NUnit.Framework;

namespace Mure.Test.Peg
{
	class PegParserTester
	{
		[Test]
		[TestCase("1+(2^5*3)")]
		public async Task TryMatch_Math(string expression)
		{
			var noAction = new Dictionary<string, PegAction>();

			var states = new PegState[]
			{
				new PegState(PegOperation.CreateSequence(new[] { new PegReference(1, null) }), noAction),
				new PegState(PegOperation.CreateSequence(new[] { new PegReference(7, "First"), new PegReference(2, "AdditiveTerms") }), CSharpAction("int", "var result = input.First; foreach (var term in input.AdditiveTerms) result += term.Item1 ? -term.Item2 : term.Item2; return result;")), // 1: sum
				new PegState(PegOperation.CreateZeroOrMore(new PegReference(3, null)), noAction),
				new PegState(PegOperation.CreateSequence(new[] { new PegReference(4, "Sign"), new PegReference(7, "Value") }), CSharpAction("(bool, int)", "return (input.Sign, input.Value);")),
				new PegState(PegOperation.CreateChoice(new[] { new PegReference(5, "Plus"), new PegReference(6, "Minus") }), CSharpAction("bool", "return input.Minus.Defined;")),
				new PegState(PegOperation.CreateCharacterSet(new[] { new PegRange('+', '+') }), noAction),
				new PegState(PegOperation.CreateCharacterSet(new[] { new PegRange('-', '-') }), noAction),
				new PegState(PegOperation.CreateSequence(new[] { new PegReference(13, null), new PegReference(8, null) }), CSharpAction("int", "return 0;")), // 7: product
				new PegState(PegOperation.CreateZeroOrMore(new PegReference(9, null)), noAction),
				new PegState(PegOperation.CreateSequence(new[] { new PegReference(10, null), new PegReference(13, null) }), noAction),
				new PegState(PegOperation.CreateChoice(new[] { new PegReference(11, null), new PegReference(12, null) }), noAction),
				new PegState(PegOperation.CreateCharacterSet(new[] { new PegRange('*', '*') }), noAction),
				new PegState(PegOperation.CreateCharacterSet(new[] { new PegRange('/', '/') }), noAction),
				new PegState(PegOperation.CreateSequence(new[] { new PegReference(17, null), new PegReference(14, null) }), noAction), // 13: power
				new PegState(PegOperation.CreateZeroOrOne(new PegReference(15, null)), noAction),
				new PegState(PegOperation.CreateSequence(new[] { new PegReference(16, null), new PegReference(13, null) }), noAction),
				new PegState(PegOperation.CreateCharacterSet(new[] { new PegRange('^', '^') }), noAction),
				new PegState(PegOperation.CreateChoice(new[] { new PegReference(18, null), new PegReference(20, null) }), noAction), // 17: value
				new PegState(PegOperation.CreateOneOrMore(new PegReference(19, null)), noAction),
				new PegState(PegOperation.CreateCharacterSet(new[] { new PegRange('0', '9') }), noAction),
				new PegState(PegOperation.CreateSequence(new[] { new PegReference(21, null), new PegReference(0, null), new PegReference(22, null) }), noAction),
				new PegState(PegOperation.CreateCharacterSet(new[] { new PegRange('(', '(') }), noAction),
				new PegState(PegOperation.CreateCharacterSet(new[] { new PegRange(')', ')') }), noAction)
			};

			// Generator
			var generator = new CSharpGenerator(states);
			string generatorCode;

			using (var writer = new StringWriter())
			{
				generator.Generate(writer, 0);

				generatorCode = writer.ToString();
			}

			var code = $"{generatorCode}\nreturn (expression) => new Parser().Parse(new System.IO.StringReader(expression));";
			var position = await CompileAndRun<int?>(code, expression);

			Assert.That(position.HasValue, Is.True);
			Assert.That(position!.Value, Is.EqualTo(expression.Length));
		}

		private static async Task<T> CompileAndRun<T>(string code, string input)
		{
			var function = await CSharpScript.EvaluateAsync<Func<string, T>>(code);

			return function(input);
		}

		private static IReadOnlyDictionary<string, PegAction> CSharpAction(string type, string body)
		{
			return new Dictionary<string, PegAction>
			{
				[CSharpGenerator.LanguageName] = new PegAction(type, body)
			};
		}
	}
}
