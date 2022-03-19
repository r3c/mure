using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Mure.Peg;

namespace Mure.Compiler.Generator
{
	class Program
	{
		private const string Preamble = @"using System.Linq;
using Mure.Peg;
";

		private static readonly PegReference Blank = new("Blank", null);
		private static readonly IReadOnlyDictionary<string, PegAction> EmptyAction = new Dictionary<string, PegAction>();

		private static readonly PegDefinition Definition = new
		(
			CSharp(new PegConfiguration(Preamble, "bool", "context")),
			new[]
			{
				new PegState(
					"Start",
					PegOperation.CreateSequence(new[] { Blank, new PegReference("State", "first"), new PegReference("StartNext", "states"), Blank }),
					CSharp(new PegAction("PegDefinition", "new PegDefinition(new Dictionary<string, PegConfiguration>(), new[] { first }.Concat(states).ToList())"))
				),

				new PegState(
					"StartNext",
					PegOperation.CreateZeroOrMore(new PegReference("StartNextState", "states")),
					EmptyAction
				),

				new PegState(
					"StartNextState",
					PegOperation.CreateSequence(new[] { Blank, new PegReference("State", "state") }),
					CSharp(new PegAction("PegState", "state"))
				),

				new PegState(
					"State",
					PegOperation.CreateSequence(new[] { new PegReference("Identifier", "key"), Blank, new PegReference("Equal", null), Blank, new PegReference("Operation", "operation"), Blank, new PegReference("ActionsOrNone", "actions") }),
					CSharp(new PegAction("PegState", "new PegState(key, /*operation*/PegOperation.CreateSequence(Array.Empty<PegReference>()), /*actions*/new Dictionary<string, PegAction>())"))
				),

				new PegState(
					"Identifier",
					PegOperation.CreateOneOrMore(new PegReference("IdentifierCharacter", "characters")),
					CSharp(new PegAction("string", "string.Join(string.Empty, characters)"))
				),

				new PegState(
					"IdentifierCharacter",
					PegOperation.CreateCharacterSet(new[] { new PegRange('_', '_'), new PegRange('0', '9'), new PegRange('A', 'Z'), new PegRange('a', 'z') }),
					EmptyAction
				),

				new PegState(
					"Equal",
					PegOperation.CreateCharacterSet(new[] { new PegRange('=', '=') }),
					EmptyAction
				),

				new PegState(
					"Operation",
					PegOperation.CreateCharacterSet(new[] { new PegRange('o', 'o') }),
					EmptyAction
				),

				new PegState(
					"ActionsOrNone",
					PegOperation.CreateCharacterSet(new[] { new PegRange('a', 'a') }),
					EmptyAction
				),

				new PegState(
					"Blank",
					PegOperation.CreateZeroOrMore(new PegReference("BlankCharacter", "characters")),
					EmptyAction
				),

				new PegState(
					"BlankCharacter",
					PegOperation.CreateCharacterSet(new[] { new PegRange(' ', ' '), new PegRange('\n', '\n'), new PegRange('\r', '\r'), new PegRange('\t', '\t') }),
					EmptyAction
				),
			}
		);

		static void Main(string[] args)
		{
			using (var writer = new StreamWriter("../../../../Mure.Compiler/Parser.cs", false, Encoding.UTF8))
			{
				writer.Write(Generate());
			}
		}

		private static string Generate()
		{
			var generator = Peg.Generator.CreateCSharp(Definition);

			using (var writer = new StringWriter())
			{
				var error = generator.Generate(writer);

				if (error.HasValue)
					throw new ArgumentOutOfRangeException(nameof(Definition));

				return writer.ToString();
			}
		}

		private static IReadOnlyDictionary<string, T> CSharp<T>(T value)
		{
			return new Dictionary<string, T>
			{
				[Peg.Generator.CSharpName] = value
			};
		}
	}
}
