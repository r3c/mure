using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Mure.Peg.Generators.CSharp;

namespace Mure.Peg.Generators
{
	class CSharpGenerator : LanguageGenerator<CSharpWriter>
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
				(generator, operation) => generator.TypeOneOrMore(operation),
				(generator, writer, operation, action) => generator.EmitOneOrMore(writer, operation, action)
			),
			new CSharpEmitter(
				(generator, operation) => generator.TypeSequence(operation),
				(generator, writer, operation, action) => generator.EmitSequence(writer, operation, action)
			),
			new CSharpEmitter(
				(generator, operation) => generator.TypeZeroOrMore(operation),
				(generator, writer, operation, action) => generator.EmitZeroOrMore(writer, operation, action)
			),
			new CSharpEmitter(
				(generator, operation) => generator.TypeZeroOrOne(operation),
				(generator, writer, operation, action) => generator.EmitZeroOrOne(writer, operation, action)
			)
		};

		public CSharpGenerator(IReadOnlyList<PegState> states) :
			base(LanguageName, states)
		{
		}

		protected override CSharpWriter CreateContext(TextWriter writer)
		{
			return new CSharpWriter(writer);
		}

		protected override void EmitFooter(CSharpWriter context)
		{
			context.EndBlock();
		}

		protected override void EmitHeader(CSharpWriter context, int startIndex)
		{
			context.WriteLine(@"// Generated code
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

class Parser");

			context.BeginBlock();

			context.WriteLine(@"public PegOption<" + GetOperationType(startIndex) + @"> Parse(TextReader reader)
{
	var stream = new PegStream(reader);
	var result = " + GetOperationName(startIndex) + @"(stream, 0);

	if (!result.HasValue)
		return " + GetCreateOptionEmpty(GetOperationType(startIndex)) + @";

	return " + GetCreateOptionValue(GetOperationType(startIndex), "result.Value.Instance") + @";
}");
		}

		protected override void EmitState(CSharpWriter context, int stateIndex)
		{
			var (operation, action) = GetState(stateIndex);
			var emitter = Emitters[(int)operation.Operator];

			context.WriteBreak();
			context.WriteLine($"private PegResult<{GetOperationType(stateIndex)}>? {GetOperationName(stateIndex)}(PegStream stream, int position)");
			context.BeginBlock();

			emitter.Write(this, context, operation, action);

			context.EndBlock();
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

		private static string GetCreateList(string type)
		{
			return $"new List<{type}>()";
		}

		private static string GetCreateOptionEmpty(string type)
		{
			return $"new PegOption<{type}> {{ Defined = false }}";
		}

		private static string GetCreateOptionValue(string type, string value)
		{
			return $"new PegOption<{type}> {{ Defined = true, Value = {value} }}";
		}

		private static string GetCreateTuple(IReadOnlyList<string> values)
		{
			// Workaround: C# doesn't support literal 0-value tuples yet
			if (values.Count < 1)
				return "ValueTuple.Create()";

			// Workaround: C# doesn't support literal 1-value named tuples yet
			if (values.Count < 2)
				return $"({values[0]}, false)";

			return $"({string.Join(", ", values)})";
		}

		private static string GetTypeTuple(IReadOnlyList<NamedType> fields)
		{
			// Workaround: C# doesn't support literal 0-value tuples yet
			if (fields.Count < 1)
				return "ValueTuple";

			// Workaround: C# doesn't support literal 1-value named tuples yet
			if (fields.Count < 2)
				return $"({fields[0].Type} {EscapeIdentifier(fields[0].Identifier)}, bool _)";

			return $"({string.Join(", ", fields.Select(element => $"{element.Type} {EscapeIdentifier(element.Identifier)}"))})";
		}

		private static string GetOperationName(int index)
		{
			return $"State{index}";
		}

		private string GetOperationType(int stateIndex)
		{
			var (operation, action) = GetState(stateIndex);

			if (action.HasValue)
				return action.Value.Type;

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
			var references = operation.References;

			for (var i = 0; i < references.Count; ++i)
			{
				var reference = references[i];
				var result = EscapeIdentifier($"result{i}");

				writer.WriteLine($"var {result} = {GetOperationName(reference.Index)}(stream, position);");
				writer.WriteBreak();
				writer.WriteLine($"if ({result}.HasValue)");
				writer.BeginBlock();

				var before = Enumerable.Range(0, i).Select(i => GetCreateOptionEmpty(GetOperationType(references[i].Index)));
				var after = Enumerable.Range(i + 1, references.Count - i - 1).Select(i => GetCreateOptionEmpty(GetOperationType(references[i].Index)));
				var values = before.Append(GetCreateOptionValue(GetOperationType(reference.Index), $"{result}.Value.Instance")).Concat(after).ToList();

				var input = GetCreateTuple(values);
				var type = TypeChoice(operation);

				EmitReturn(writer, $"{result}.Value.Position", type, input, action);

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
			writer.WriteLine($"var instances = {GetCreateList(GetOperationType(operation.References[0].Index))};");
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

			EmitReturn(writer, "position", TypeOneOrMore(operation), "instances", action);

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

			var input = GetCreateTuple(elements.Select(element => element.Identifier).ToList());
			var type = TypeSequence(operation);

			EmitReturn(writer, "position", type, input, action);
		}

		private void EmitZeroOrMore(CSharpWriter writer, PegOperation operation, PegAction? action)
		{
			var index = operation.References[0].Index;

			writer.WriteLine($"var instances = {GetCreateList(GetOperationType(operation.References[0].Index))};");
			writer.WriteBreak();
			writer.WriteLine("while (true)");
			writer.BeginBlock();
			writer.WriteLine($"var next = {GetOperationName(index)}(stream, position);");
			writer.WriteBreak();
			writer.WriteLine("if (!next.HasValue)");
			writer.BeginBlock();

			EmitReturn(writer, "position", TypeZeroOrMore(operation), "instances", action);

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
			return GetTypeTuple(operation.References
				.Select((reference, order) => new NamedType($"PegOption<{GetOperationType(reference.Index)}>", reference.Identifier ?? $"choice{order}"))
				.ToList());
		}

		private string TypeOneOrMore(PegOperation operation)
		{
			return TypeZeroOrMore(operation);
		}

		private string TypeSequence(PegOperation operation)
		{
			return GetTypeTuple(operation.References
				.Select((reference, order) => new NamedType(GetOperationType(reference.Index), reference.Identifier ?? $"sequence{order}"))
				.ToList());
		}

		private string TypeZeroOrMore(PegOperation operation)
		{
			return $"IReadOnlyList<{GetOperationType(operation.References[0].Index)}>";
		}

		private string TypeZeroOrOne(PegOperation operation)
		{
			return $"PegOption<{GetOperationType(operation.References[0].Index)}>";
		}

		private readonly struct NamedType
		{
			public readonly string Identifier;
			public readonly string Type;

			public NamedType(string type, string identifier)
			{
				Identifier = identifier;
				Type = type;
			}
		}
	}
}
