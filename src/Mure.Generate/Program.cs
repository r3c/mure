using System.Collections.Generic;
using System.IO;
using System.Text;
using Mure.Peg;

namespace Mure.Generate
{
	class Program
	{
		private static readonly IReadOnlyDictionary<string, PegAction> EmptyAction = new Dictionary<string, PegAction>();

		private static readonly PegDefinition Definition = new("bool", "main", new[]
		{
			new PegState("main", PegOperation.CreateSequence(new[] { new PegReference("number", "result") }), CSharpAction("int", "return int.Parse(result);")),
			new PegState("number", PegOperation.CreateCharacterSet(new[] { new PegRange('0', '9') }), EmptyAction)
		});

		static void Main(string[] args)
		{
			using (var writer = new StreamWriter("Parser.cs", false, Encoding.UTF8))
			{
				writer.Write(Generate());
			}
		}

		private static string Generate()
		{
			var generator = Generator.CreateCSharp(Definition);

			using (var writer = new StringWriter())
			{
				generator.Generate(writer);

				return writer.ToString();
			}
		}

		private static IReadOnlyDictionary<string, PegAction> CSharpAction(string type, string body)
		{
			return new Dictionary<string, PegAction>
			{
				[Generator.CSharpName] = new PegAction(type, body)
			};
		}
	}
}
