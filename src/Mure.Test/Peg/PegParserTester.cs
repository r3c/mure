using System;
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
		[TestCase("1+(2^5*3)")]
		public async Task TryMatch_Math(string expression)
		{
			var states = new PegState[]
			{
				PegState.CreateSequence(new[] { 1 }), // 0: start
				PegState.CreateSequence(new[] { 7, 2 }), // 1: sum
				PegState.CreateZeroOrMore(3),
				PegState.CreateSequence(new[] { 4, 7 }),
				PegState.CreateChoice(new[] { 5, 6 }),
				PegState.CreateCharacterSet(new[] { new PegRange('+', '+') }),
				PegState.CreateCharacterSet(new[] { new PegRange('-', '-') }),
				PegState.CreateSequence(new[] { 13, 8 }), // 7: product
				PegState.CreateZeroOrMore(9),
				PegState.CreateSequence(new[] { 10, 13 }),
				PegState.CreateChoice(new[] { 11, 12 }),
				PegState.CreateCharacterSet(new[] { new PegRange('*', '*') }),
				PegState.CreateCharacterSet(new[] { new PegRange('/', '/') }),
				PegState.CreateSequence(new[] { 17, 14 }), // 13: power
				PegState.CreateZeroOrOne(15),
				PegState.CreateSequence(new[] { 16, 13 }),
				PegState.CreateCharacterSet(new[] { new PegRange('^', '^') }),
				PegState.CreateChoice(new[] { 18, 20 }), // 17: value
				PegState.CreateOneOrMore(19),
				PegState.CreateCharacterSet(new[] { new PegRange('0', '9') }),
				PegState.CreateSequence(new[] { 21, 0, 22 }),
				PegState.CreateCharacterSet(new[] { new PegRange('(', '(') }),
				PegState.CreateCharacterSet(new[] { new PegRange(')', ')') })
			};

			// Generator
			var generator = new PegGenerator();
			string generatorCode;

			using (var writer = new StringWriter())
			{
				generator.Generate(writer, states, 0);

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
	}
}
