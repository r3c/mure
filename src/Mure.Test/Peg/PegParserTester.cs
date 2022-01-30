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
				new PegState(PegOperation.CreateSequence(new[] { 1 }), null),
				new PegState(PegOperation.CreateSequence(new[] { 7, 2 }), "sum"), // 1: sum
				new PegState(PegOperation.CreateZeroOrMore(3), null),
				new PegState(PegOperation.CreateSequence(new[] { 4, 7 }), null),
				new PegState(PegOperation.CreateChoice(new[] { 5, 6 }), null),
				new PegState(PegOperation.CreateCharacterSet(new[] { new PegRange('+', '+') }), null),
				new PegState(PegOperation.CreateCharacterSet(new[] { new PegRange('-', '-') }), null),
				new PegState(PegOperation.CreateSequence(new[] { 13, 8 }), "product"), // 7: product
				new PegState(PegOperation.CreateZeroOrMore(9), null),
				new PegState(PegOperation.CreateSequence(new[] { 10, 13 }), null),
				new PegState(PegOperation.CreateChoice(new[] { 11, 12 }), null),
				new PegState(PegOperation.CreateCharacterSet(new[] { new PegRange('*', '*') }), null),
				new PegState(PegOperation.CreateCharacterSet(new[] { new PegRange('/', '/') }), null),
				new PegState(PegOperation.CreateSequence(new[] { 17, 14 }), "power"), // 13: power
				new PegState(PegOperation.CreateZeroOrOne(15), null),
				new PegState(PegOperation.CreateSequence(new[] { 16, 13 }), null),
				new PegState(PegOperation.CreateCharacterSet(new[] { new PegRange('^', '^') }), null),
				new PegState(PegOperation.CreateChoice(new[] { 18, 20 }), "value"), // 17: value
				new PegState(PegOperation.CreateOneOrMore(19), null),
				new PegState(PegOperation.CreateCharacterSet(new[] { new PegRange('0', '9') }), null),
				new PegState(PegOperation.CreateSequence(new[] { 21, 0, 22 }), null),
				new PegState(PegOperation.CreateCharacterSet(new[] { new PegRange('(', '(') }), null),
				new PegState(PegOperation.CreateCharacterSet(new[] { new PegRange(')', ')') }), null)
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
