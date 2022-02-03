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
		[TestCase("1+(2^5*3)", 97)]
		public async Task TryMatch_Math(string expression, int expected)
		{
			var noAction = new Dictionary<string, PegAction>();

			var states = new PegState[]
			{
				new PegState(PegOperation.CreateSequence(new[] { new PegReference(1, null) }), noAction),
				new PegState(PegOperation.CreateSequence(new[] { new PegReference(7, "First"), new PegReference(2, "AdditiveTerms") }), CSharpAction("int", "var result = input.First; foreach (var term in input.AdditiveTerms) result = term.Item1 ? result - term.Item2 : result + term.Item2; return result;")), // 1: sum
				new PegState(PegOperation.CreateZeroOrMore(new PegReference(3, null)), noAction),
				new PegState(PegOperation.CreateSequence(new[] { new PegReference(4, "Sign"), new PegReference(7, "Value") }), CSharpAction("(bool, int)", "return (input.Sign, input.Value);")),
				new PegState(PegOperation.CreateChoice(new[] { new PegReference(5, "Plus"), new PegReference(6, "Minus") }), CSharpAction("bool", "return input.Minus.Defined;")),
				new PegState(PegOperation.CreateCharacterSet(new[] { new PegRange('+', '+') }), noAction),
				new PegState(PegOperation.CreateCharacterSet(new[] { new PegRange('-', '-') }), noAction),
				new PegState(PegOperation.CreateSequence(new[] { new PegReference(13, "First"), new PegReference(8, "MultiplicativeTerms") }), CSharpAction("int", "var result = input.First; foreach (var term in input.MultiplicativeTerms) result = term.Item1 ? result / term.Item2 : result * term.Item2; return result;")), // 7: product
				new PegState(PegOperation.CreateZeroOrMore(new PegReference(9, null)), noAction),
				new PegState(PegOperation.CreateSequence(new[] { new PegReference(10, "Sign"), new PegReference(13, "Value") }), CSharpAction("(bool, int)", "return (input.Sign, input.Value);")),
				new PegState(PegOperation.CreateChoice(new[] { new PegReference(11, "Multiply"), new PegReference(12, "Divide") }), CSharpAction("bool", "return input.Divide.Defined;")),
				new PegState(PegOperation.CreateCharacterSet(new[] { new PegRange('*', '*') }), noAction),
				new PegState(PegOperation.CreateCharacterSet(new[] { new PegRange('/', '/') }), noAction),
				new PegState(PegOperation.CreateSequence(new[] { new PegReference(17, "Value"), new PegReference(14, "Power") }), CSharpAction("int", "return input.Power.Defined ? (int)Math.Pow(input.Value, input.Power.Value) : input.Value;")), // 13: power
				new PegState(PegOperation.CreateZeroOrOne(new PegReference(15, null)), noAction),
				new PegState(PegOperation.CreateSequence(new[] { new PegReference(16, null), new PegReference(13, "Power") }), CSharpAction("int", "return input.Power;")),
				new PegState(PegOperation.CreateCharacterSet(new[] { new PegRange('^', '^') }), noAction),
				new PegState(PegOperation.CreateChoice(new[] { new PegReference(18, "Number"), new PegReference(20, "Parenthesis") }), CSharpAction("int", "return input.Number.Defined ? input.Number.Value : input.Parenthesis.Value;")), // 17: value
				new PegState(PegOperation.CreateOneOrMore(new PegReference(19, "Digits")), CSharpAction("int", "return int.Parse(string.Join(string.Empty, input));")),
				new PegState(PegOperation.CreateCharacterSet(new[] { new PegRange('0', '9') }), noAction),
				new PegState(PegOperation.CreateSequence(new[] { new PegReference(21, null), new PegReference(0, "Expression"), new PegReference(22, null) }), CSharpAction("int", "return input.Expression;")),
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

			var code = $"{generatorCode}\nreturn (expression) => new Parser().Parse(new System.IO.StringReader(expression)).Value;";
			var result = await CompileAndRun<int>(code, expression);

			Assert.That(result, Is.EqualTo(expected));
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
