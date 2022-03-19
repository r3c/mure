using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Mure.Peg;
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
			var definition = new PegDefinition
			(
				CSharp(new PegConfiguration(string.Empty, "bool", "context")),
				new[]
				{
					new PegState("expression", PegOperation.CreateSequence(new[] { new PegReference("sum", "result") }), CSharp(new PegAction("int", "result"))),
					new PegState("sum", PegOperation.CreateSequence(new[] { new PegReference("product", "first"), new PegReference("additiveTerms", "terms") }), CSharp(new PegAction("int", "{ var result = first; foreach (var term in terms) result = term.Item1 ? result - term.Item2 : result + term.Item2; return result; }"))),
					new PegState("additiveTerms", PegOperation.CreateZeroOrMore(new PegReference("additiveTerm", null)), noAction),
					new PegState("additiveTerm", PegOperation.CreateSequence(new[] { new PegReference("4", "sign"), new PegReference("product", "value") }), CSharp(new PegAction("(bool, int)", "(sign, value)"))),
					new PegState("4", PegOperation.CreateChoice(new[] { new PegReference("5", "plus"), new PegReference("6", "minus") }), CSharp(new PegAction("bool", "minus.Defined"))),
					new PegState("5", PegOperation.CreateCharacterSet(new[] { new PegRange('+', '+') }), noAction),
					new PegState("6", PegOperation.CreateCharacterSet(new[] { new PegRange('-', '-') }), noAction),
					new PegState("product", PegOperation.CreateSequence(new[] { new PegReference("power", "first"), new PegReference("multiplicativeTerms", "terms") }), CSharp(new PegAction("int", "{ var result = first; foreach (var term in terms) result = term.Item1 ? result / term.Item2 : result * term.Item2; return result; }"))),
					new PegState("multiplicativeTerms", PegOperation.CreateZeroOrMore(new PegReference("multiplicativeTerm", null)), noAction),
					new PegState("multiplicativeTerm", PegOperation.CreateSequence(new[] { new PegReference("10", "sign"), new PegReference("power", "value") }), CSharp(new PegAction("(bool, int)", "(sign, value)"))),
					new PegState("10", PegOperation.CreateChoice(new[] { new PegReference("11", "multiply"), new PegReference("12", "divide") }), CSharp(new PegAction("bool", "divide.Defined"))),
					new PegState("11", PegOperation.CreateCharacterSet(new[] { new PegRange('*', '*') }), noAction),
					new PegState("12", PegOperation.CreateCharacterSet(new[] { new PegRange('/', '/') }), noAction),
					new PegState("power", PegOperation.CreateSequence(new[] { new PegReference("value", "value"), new PegReference("14", "power") }), CSharp(new PegAction("int", "power.Defined ? (int)Math.Pow(value, power.Value) : value"))),
					new PegState("14", PegOperation.CreateZeroOrOne(new PegReference("15", null)), noAction),
					new PegState("15", PegOperation.CreateSequence(new[] { new PegReference("16", null), new PegReference("power", "power") }), CSharp(new PegAction("int", "power"))),
					new PegState("16", PegOperation.CreateCharacterSet(new[] { new PegRange('^', '^') }), noAction),
					new PegState("value", PegOperation.CreateChoice(new[] { new PegReference("18", "number"), new PegReference("20", "parenthesis") }), CSharp(new PegAction("int", "number.Defined ? number.Value : parenthesis.Value"))),
					new PegState("18", PegOperation.CreateOneOrMore(new PegReference("19", "digits")), CSharp(new PegAction("int", "int.Parse(string.Join(string.Empty, digits))"))),
					new PegState("19", PegOperation.CreateCharacterSet(new[] { new PegRange('0', '9') }), noAction),
					new PegState("20", PegOperation.CreateSequence(new[] { new PegReference("21", null), new PegReference("expression", "expression"), new PegReference("22", null) }), CSharp(new PegAction("int", "expression"))),
					new PegState("21", PegOperation.CreateCharacterSet(new[] { new PegRange('(', '(') }), noAction),
					new PegState("22", PegOperation.CreateCharacterSet(new[] { new PegRange(')', ')') }), noAction)
				}
			);

			// Generator
			var generator = Generator.CreateCSharp(definition);
			string generatorCode;

			using (var writer = new StringWriter())
			{
				Assert.That(generator.Generate(writer).HasValue, Is.False);

				generatorCode = writer.ToString();
			}

			var code = $"{generatorCode}\nreturn (expression) => new Parser().Parse(new System.IO.StringReader(expression), false).Value;";
			var result = await CompileAndRun<int>(code, expression);

			Assert.That(result, Is.EqualTo(expected));
		}

		private static async Task<T> CompileAndRun<T>(string code, string input)
		{
			var function = await CSharpScript.EvaluateAsync<Func<string, T>>(code);

			return function(input);
		}

		private static IReadOnlyDictionary<string, T> CSharp<T>(T value)
		{
			return new Dictionary<string, T>
			{
				[Generator.CSharpName] = value
			};
		}
	}
}
