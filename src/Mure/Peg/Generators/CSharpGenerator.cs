﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Mure.Peg.Generators.CSharp;

namespace Mure.Peg.Generators
{
	class CSharpGenerator : IGenerator
	{
		public const string LanguageName = "csharp";

		private static readonly IReadOnlyList<CSharpEmitter> Emitters = new[]
		{
			new CSharpEmitter(
				(generator, operation) => generator.TypeCharacterSet(),
				(generator, writer, operation, action) => generator.EmitCharacterSet(writer, operation, action)
			),
			new CSharpEmitter(
				(generator, operation) => generator.TypeChoice(operation),
				(generator, writer, operation, action) => generator.EmitChoice(writer, operation, action)
			),
			new CSharpEmitter(
				(generator, operation) => generator.TypeOneOrMore(operation, false),
				(generator, writer, operation, action) => generator.EmitOneOrMore(writer, operation, action)
			),
			new CSharpEmitter(
				(generator, operation) => generator.TypeSequence(operation),
				(generator, writer, operation, action) => generator.EmitSequence(writer, operation, action)
			),
			new CSharpEmitter(
				(generator, operation) => generator.TypeZeroOrMore(operation, false),
				(generator, writer, operation, action) => generator.EmitZeroOrMore(writer, operation, action)
			),
			new CSharpEmitter(
				(generator, operation) => generator.TypeZeroOrOne(operation),
				(generator, writer, operation, action) => generator.EmitZeroOrOne(writer, operation, action)
			)
		};

		private readonly IReadOnlyList<PegState> _states;

		public CSharpGenerator(IReadOnlyList<PegState> states)
		{
			_states = states;
		}

		public void Generate(TextWriter writer, int startIndex)
		{
			writer.WriteLine(@"// Generated code
using System;
using System.Collections.Generic;
using System.IO;

struct PegOption<T>
{
	public bool Defined;
	public T Value;
}

readonly struct PegResult<T>
{
	public readonly T Instance;
	public readonly int Position;

	public PegResult(T instance, int position)
	{
		Instance = instance;
		Position = position;
	}
}

class PegStream
{
	private readonly List<int> _buffer;
	private readonly TextReader _reader;

	public PegStream(TextReader reader)
	{
		_buffer = new List<int>();
		_reader = reader;
	}

	public int ReadAt(int position)
	{
		while (_buffer.Count <= position)
			_buffer.Add(_reader.Read());

		return _buffer[position];
	}
}

class Parser
{
	public PegOption<" + GetOperationType(startIndex) + @"> Parse(TextReader reader)
	{
		var stream = new PegStream(reader);
		var result = " + GetOperationName(startIndex) + @"(stream, 0);

		if (!result.HasValue)
			return " + GetCreateOptionEmpty(GetOperationType(startIndex)) + @";

		return " + GetCreateOptionValue(GetOperationType(startIndex), "result.Value.Instance") + @";
	}");

			var pegWriter = new CSharpWriter(writer, 1);

			for (var i = 0; i < _states.Count; ++i)
			{
				var state = _states[i];
				var operation = state.Operation;
				var emitter = Emitters[(int)operation.Operator];

				pegWriter.WriteBreak();
				pegWriter.WriteLine($"private PegResult<{GetOperationType(i)}>? {GetOperationName(i)}(PegStream stream, int position)");
				pegWriter.BeginBlock();

				emitter.Write(this, pegWriter, operation, state.Actions.TryGetValue(LanguageName, out var action) ? action : null);

				pegWriter.EndBlock();
			}

			writer.Write(@"
}");
		}

		private static void EmitReturn(CSharpWriter writer, string position, string sourceType, string input, PegAction? action)
		{
			if (action.HasValue)
			{
				writer.WriteLine($"Func<{sourceType}, {action.Value.Type}> converter = (input) => {{ {action.Value.Body} }};");
				writer.WriteLine($"return new PegResult<{action.Value.Type}>(converter({input}), {position});");
			}
			else
				writer.WriteLine($"return new PegResult<{sourceType}>({input}, {position});");
		}

		private static string EscapeIdentifier(string identifier)
		{
			return identifier; // FIXME
		}

		private static string GetCreateOptionEmpty(string type)
		{
			return $"new PegOption<{type}> {{ Defined = false }}";
		}

		private static string GetCreateOptionValue(string type, string value)
		{
			return $"new PegOption<{type}> {{ Defined = true, Value = {value} }}";
		}

		private static string GetOperationName(int index)
		{
			return $"State{index}";
		}

		private string GetOperationType(int index)
		{
			var state = _states[index];

			if (state.Actions.TryGetValue(LanguageName, out var action))
				return action.Type;

			var operation = state.Operation;
			var emitter = Emitters[(int)operation.Operator];

			return emitter.Infer(this, operation);
		}

		private void EmitCharacterSet(CSharpWriter writer, PegOperation operation, PegAction? action)
		{
			var buffer = new StringBuilder();
			var next = string.Empty;

			foreach (var range in operation.CharacterRanges)
			{
				buffer.Append($"{next}(character >= {(int)range.Begin} && character <= {(int)range.End})");

				next = " || ";
			}

			writer.WriteLine("var character = stream.ReadAt(position);");
			writer.WriteBreak();
			writer.WriteLine($"if ({buffer})");
			writer.BeginBlock();

			EmitReturn(writer, "position + 1", TypeCharacterSet(), "new string((char)character, 1)", action);

			writer.EndBlock();
			writer.WriteBreak();
			writer.WriteLine("return null;");
		}

		private void EmitChoice(CSharpWriter writer, PegOperation operation, PegAction? action)
		{
			var identifiers = operation.References
				.Select(reference => EscapeIdentifier(reference.Identifier ?? $"choice{reference.Index}"))
				.ToList();

			var references = operation.References;

			for (var i = 0; i < references.Count; ++i)
			{
				var identifier = identifiers[i];
				var reference = references[i];
				var result = EscapeIdentifier($"result{i}");

				writer.WriteLine($"var {result} = {GetOperationName(reference.Index)}(stream, position);");
				writer.WriteBreak();
				writer.WriteLine($"if ({result}.HasValue)");
				writer.BeginBlock();

				string input;

				if (references.Count < 2)
				{
					input = GetCreateOptionValue(GetOperationType(reference.Index), $"{result}.Value.Instance");
				}
				else
				{
					var before = Enumerable.Range(0, i).Select(i => GetCreateOptionEmpty(GetOperationType(references[i].Index)));
					var after = Enumerable.Range(i + 1, references.Count - i - 1).Select(i => GetCreateOptionEmpty(GetOperationType(references[i].Index)));
					var values = before.Append(GetCreateOptionValue(GetOperationType(reference.Index), $"{result}.Value.Instance")).Concat(after);

					input = $"({string.Join(", ", values)})";
				}

				EmitReturn(writer, $"{result}.Value.Position", TypeChoice(operation), input, action);

				writer.EndBlock();
				writer.WriteBreak();
			}

			writer.WriteLine("return null;");
		}

		private void EmitOneOrMore(CSharpWriter writer, PegOperation operation, PegAction? action)
		{
			var index = operation.References[0].Index;

			writer.WriteLine($"var first = {GetOperationName(index)}(stream, position);");
			writer.WriteBreak();
			writer.WriteLine("if (!first.HasValue)");
			writer.BeginBlock();
			writer.WriteLine("return null;");
			writer.EndBlock();
			writer.WriteBreak();
			writer.WriteLine($"var instances = new {TypeOneOrMore(operation, true)}();");
			writer.WriteBreak();
			writer.WriteLine("instances.Add(first.Value.Instance);");
			writer.WriteBreak();
			writer.WriteLine("position = first.Value.Position;");
			writer.WriteBreak();
			writer.WriteLine("while (true)"); // FIXME: similar to EmitZeroOrMore
			writer.BeginBlock();
			writer.WriteLine($"var next = {GetOperationName(index)}(stream, position);");
			writer.WriteBreak();
			writer.WriteLine("if (!next.HasValue)");
			writer.BeginBlock();

			EmitReturn(writer, "position", TypeOneOrMore(operation, false), "instances", action);

			writer.EndBlock();
			writer.WriteBreak();
			writer.WriteLine("instances.Add(next.Value.Instance);");
			writer.WriteBreak();
			writer.WriteLine("position = next.Value.Position;");
			writer.EndBlock();
		}

		private void EmitSequence(CSharpWriter writer, PegOperation operation, PegAction? action)
		{
			var elements = operation.References
				.Select((reference, order) => new { Identifier = EscapeIdentifier(reference.Identifier ?? $"sequence{order}"), reference.Index })
				.ToList();

			var i = 0;

			foreach (var element in elements)
			{
				var name = $"result{i++}";

				writer.WriteLine($"var {name} = {GetOperationName(element.Index)}(stream, position);");
				writer.WriteBreak();
				writer.WriteLine($"if (!{name}.HasValue)");
				writer.BeginBlock();
				writer.WriteLine($"return null;");
				writer.EndBlock();
				writer.WriteBreak();
				writer.WriteLine($"var {element.Identifier} = {name}.Value.Instance;");
				writer.WriteBreak();
				writer.WriteLine($"position = {name}.Value.Position;");
				writer.WriteBreak();
			}

			string input;

			if (elements.Count < 1)
				input = "false";
			else if (elements.Count < 2)
				input = elements[0].Identifier;
			else
				input = $"({string.Join(", ", elements.Select(element => element.Identifier))})";

			EmitReturn(writer, "position", TypeSequence(operation), input, action);
		}

		private void EmitZeroOrMore(CSharpWriter writer, PegOperation operation, PegAction? action)
		{
			var index = operation.References[0].Index;

			writer.WriteLine($"var instances = new {TypeZeroOrMore(operation, true)}();");
			writer.WriteBreak();
			writer.WriteLine("while (true)");
			writer.BeginBlock();
			writer.WriteLine($"var next = {GetOperationName(index)}(stream, position);");
			writer.WriteBreak();
			writer.WriteLine("if (!next.HasValue)");
			writer.BeginBlock();

			EmitReturn(writer, "position", TypeZeroOrMore(operation, false), "instances", action);

			writer.EndBlock();
			writer.WriteBreak();
			writer.WriteLine("instances.Add(next.Value.Instance);");
			writer.WriteBreak();
			writer.WriteLine("position = next.Value.Position;");
			writer.EndBlock();
		}

		private void EmitZeroOrOne(CSharpWriter writer, PegOperation operation, PegAction? action)
		{
			var index = operation.References[0].Index;
			var type = TypeZeroOrOne(operation);

			writer.WriteLine($"var one = {GetOperationName(index)}(stream, position);");
			writer.WriteLine($"{type} instance;");
			writer.WriteBreak();
			writer.WriteLine("if (one.HasValue)");
			writer.BeginBlock();
			writer.WriteLine($"instance = new {type} {{ Defined = true, Value = one.Value.Instance }};");
			writer.WriteLine($"position = one.Value.Position;");
			writer.EndBlock();
			writer.WriteLine("else");
			writer.BeginBlock();
			writer.WriteLine($"instance = new {type} {{ Defined = false }};");
			writer.EndBlock();
			writer.WriteBreak();

			EmitReturn(writer, "position", type, "instance", action);
		}

		private string TypeCharacterSet()
		{
			return "string";
		}

		private string TypeChoice(PegOperation operation)
		{
			var elements = operation.References
				.Select((reference, order) => new
				{
					Identifier = EscapeIdentifier(reference.Identifier ?? $"choice{order}"),
					Type = $"PegOption<{GetOperationType(reference.Index)}>"
				})
				.ToList();

			if (elements.Count < 1)
				return "bool";

			if (elements.Count < 2)
				return elements[0].Type;

			return $"({string.Join(", ", elements.Select(element => $"{element.Type} {element.Identifier}"))})";
		}

		private string TypeOneOrMore(PegOperation operation, bool concrete)
		{
			return TypeZeroOrMore(operation, concrete);
		}

		private string TypeSequence(PegOperation operation)
		{
			var elements = operation.References
				.Select((reference, order) => new
				{
					Identifier = EscapeIdentifier(reference.Identifier ?? $"sequence{order}"),
					Type = GetOperationType(reference.Index)
				})
				.ToList();

			if (elements.Count < 1)
				return "bool";

			if (elements.Count < 2)
				return elements[0].Type;

			return $"({string.Join(", ", elements.Select(element => $"{element.Type} {element.Identifier}"))})";
		}

		private string TypeZeroOrMore(PegOperation operation, bool concrete)
		{
			return $"{(concrete ? "List" : "IReadOnlyList")}<{GetOperationType(operation.References[0].Index)}>";
		}

		private string TypeZeroOrOne(PegOperation operation)
		{
			return $"PegOption<{GetOperationType(operation.References[0].Index)}>";
		}
	}
}