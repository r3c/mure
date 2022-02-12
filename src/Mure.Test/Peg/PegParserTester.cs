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
			var definition = new PegDefinition(new[]
			{
				new PegState(PegOperation.CreateSequence(new[] { new PegReference(1, "result") }), CSharpAction("int", "return result;")),
				new PegState(PegOperation.CreateSequence(new[] { new PegReference(7, "first"), new PegReference(2, "additiveTerms") }), CSharpAction("int", "var result = first; foreach (var term in additiveTerms) result = term.Item1 ? result - term.Item2 : result + term.Item2; return result;")), // 1: sum
				new PegState(PegOperation.CreateZeroOrMore(new PegReference(3, null)), noAction),
				new PegState(PegOperation.CreateSequence(new[] { new PegReference(4, "sign"), new PegReference(7, "value") }), CSharpAction("(bool, int)", "return (sign, value);")),
				new PegState(PegOperation.CreateChoice(new[] { new PegReference(5, "plus"), new PegReference(6, "minus") }), CSharpAction("bool", "return minus.Defined;")),
				new PegState(PegOperation.CreateCharacterSet(new[] { new PegRange('+', '+') }), noAction),
				new PegState(PegOperation.CreateCharacterSet(new[] { new PegRange('-', '-') }), noAction),
				new PegState(PegOperation.CreateSequence(new[] { new PegReference(13, "first"), new PegReference(8, "multiplicativeTerms") }), CSharpAction("int", "var result = first; foreach (var term in multiplicativeTerms) result = term.Item1 ? result / term.Item2 : result * term.Item2; return result;")), // 7: product
				new PegState(PegOperation.CreateZeroOrMore(new PegReference(9, null)), noAction),
				new PegState(PegOperation.CreateSequence(new[] { new PegReference(10, "sign"), new PegReference(13, "value") }), CSharpAction("(bool, int)", "return (sign, value);")),
				new PegState(PegOperation.CreateChoice(new[] { new PegReference(11, "multiply"), new PegReference(12, "divide") }), CSharpAction("bool", "return divide.Defined;")),
				new PegState(PegOperation.CreateCharacterSet(new[] { new PegRange('*', '*') }), noAction),
				new PegState(PegOperation.CreateCharacterSet(new[] { new PegRange('/', '/') }), noAction),
				new PegState(PegOperation.CreateSequence(new[] { new PegReference(17, "value"), new PegReference(14, "power") }), CSharpAction("int", "return power.Defined ? (int)Math.Pow(value, power.Value) : value;")), // 13: power
				new PegState(PegOperation.CreateZeroOrOne(new PegReference(15, null)), noAction),
				new PegState(PegOperation.CreateSequence(new[] { new PegReference(16, null), new PegReference(13, "power") }), CSharpAction("int", "return power;")),
				new PegState(PegOperation.CreateCharacterSet(new[] { new PegRange('^', '^') }), noAction),
				new PegState(PegOperation.CreateChoice(new[] { new PegReference(18, "number"), new PegReference(20, "parenthesis") }), CSharpAction("int", "return number.Defined ? number.Value : parenthesis.Value;")), // 17: value
				new PegState(PegOperation.CreateOneOrMore(new PegReference(19, "digits")), CSharpAction("int", "return int.Parse(string.Join(string.Empty, digits));")),
				new PegState(PegOperation.CreateCharacterSet(new[] { new PegRange('0', '9') }), noAction),
				new PegState(PegOperation.CreateSequence(new[] { new PegReference(21, null), new PegReference(0, "expression"), new PegReference(22, null) }), CSharpAction("int", "return expression;")),
				new PegState(PegOperation.CreateCharacterSet(new[] { new PegRange('(', '(') }), noAction),
				new PegState(PegOperation.CreateCharacterSet(new[] { new PegRange(')', ')') }), noAction)
			}, 0);

			// Generator
			var generator = new CSharpGenerator(definition);
			string generatorCode;

			using (var writer = new StringWriter())
			{
				generator.Generate(writer);

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
