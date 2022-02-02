using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mure.Peg.Generators.CSharp;

namespace Mure.Peg.Generators
{
	class CSharpGenerator : IGenerator
	{
		private static readonly IReadOnlyList<CSharpEmitter> Emitters = new[]
		{
			new CSharpEmitter(
				(generator, operation) => generator.TypeCharacterSet(),
				(generator, writer, operation) => generator.EmitCharacterSet(writer, operation)
			),
			new CSharpEmitter(
				(generator, operation) => generator.TypeChoice(operation),
				(generator, writer, operation) => generator.EmitChoice(writer, operation)
			),
			new CSharpEmitter(
				(generator, operation) => generator.TypeOneOrMore(operation, false),
				(generator, writer, operation) => generator.EmitOneOrMore(writer, operation)
			),
			new CSharpEmitter(
				(generator, operation) => generator.TypeSequence(operation),
				(generator, writer, operation) => generator.EmitSequence(writer, operation)
			),
			new CSharpEmitter(
				(generator, operation) => generator.TypeZeroOrMore(operation, false),
				(generator, writer, operation) => generator.EmitZeroOrMore(writer, operation)
			),
			new CSharpEmitter(
				(generator, operation) => generator.TypeZeroOrOne(operation),
				(generator, writer, operation) => generator.EmitZeroOrOne(writer, operation)
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
	public int? Parse(TextReader reader)
	{
		var stream = new PegStream(reader);

		return " + GetOperationName(startIndex) + @"(stream, 0)?.Position;
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

				emitter.Write(this, pegWriter, operation);

				pegWriter.EndBlock();
			}

			writer.Write(@"
}");
		}

		private static string EscapeIdentifier(string identifier)
		{
			return identifier; // FIXME
		}

		private static string GetOperationName(int index)
		{
			return $"State{index}";
		}

		private string GetOperationType(int index)
		{
			var state = _states[index];

			if (state.Type is not null)
				return state.Type;

			var operation = state.Operation;
			var emitter = Emitters[(int)operation.Operator];

			return emitter.Infer(this, operation);
		}

		private void EmitCharacterSet(CSharpWriter writer, PegOperation operation)
		{
			writer.WriteLine("var character = stream.ReadAt(position);");
			writer.WriteBreak();
			writer.WriteLine("if (");

			var separator = string.Empty;

			foreach (var range in operation.CharacterRanges)
			{
				writer.WriteLine($"{separator}(character >= {(int)range.Begin} && character <= {(int)range.End})");

				separator = "|| ";
			}

			writer.WriteLine(")");
			writer.BeginBlock();
			writer.WriteLine($"return new PegResult<{TypeCharacterSet()}>(new string((char)character, 1), position + 1);");
			writer.EndBlock();
			writer.WriteBreak();
			writer.WriteLine("return null;");
		}

		private void EmitChoice(CSharpWriter writer, PegOperation operation)
		{
			var identifiers = operation.References
				.Select((reference, index) => new { reference.Identifier, Index = index })
				.Where(tuple => tuple.Identifier is not null)
				.Select(tuple => new { Identifier = EscapeIdentifier(tuple.Identifier!), tuple.Index })
				.ToList();

			for (var i = 0; i < operation.References.Count; ++i)
			{
				var reference = operation.References[i];
				var identifier = EscapeIdentifier(reference.Identifier ?? $"choice{reference.Index}");

				writer.WriteLine($"var {identifier} = {GetOperationName(reference.Index)}(stream, position);");
				writer.WriteBreak();
				writer.WriteLine($"if ({identifier}.HasValue)");
				writer.BeginBlock();
				writer.WriteLine($"return new PegResult<{TypeChoice(operation)}>({(identifiers.Count > 0 ? $"({string.Join(", ", identifiers.Select(tuple => tuple.Index == i ? tuple.Identifier : "null"))})" : "false")}, {identifier}.Value.Position);");
				writer.EndBlock();
				writer.WriteBreak();
			}

			writer.WriteLine("return null;");
		}

		private void EmitOneOrMore(CSharpWriter writer, PegOperation operation)
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
			writer.WriteLine($"return new PegResult<{TypeOneOrMore(operation, false)}>(instances, position);");
			writer.EndBlock();
			writer.WriteBreak();
			writer.WriteLine("instances.Add(next.Value.Instance);");
			writer.WriteBreak();
			writer.WriteLine("position = next.Value.Position;");
			writer.EndBlock();
		}

		private void EmitSequence(CSharpWriter writer, PegOperation operation)
		{
			var i = 0;

			foreach (var reference in operation.References)
			{
				var name = $"result{i++}";

				writer.WriteLine($"var {name} = {GetOperationName(reference.Index)}(stream, position);");
				writer.WriteBreak();
				writer.WriteLine($"if (!{name}.HasValue)");
				writer.BeginBlock();
				writer.WriteLine($"return null;");
				writer.EndBlock();

				if (reference.Identifier is not null)
				{
					writer.WriteBreak();
					writer.WriteLine($"var {EscapeIdentifier(reference.Identifier)} = {name}.Value.Instance;");
				}

				writer.WriteBreak();
				writer.WriteLine($"position = {name}.Value.Position;");
				writer.WriteBreak();
			}

			var identifiers = operation.References
				.Where(reference => reference.Identifier is not null)
				.Select(reference => EscapeIdentifier(reference.Identifier!))
				.ToList();

			writer.WriteLine($"return new PegResult<{TypeSequence(operation)}>({(identifiers.Count > 0 ? $"({string.Join(", ", identifiers)})" : "false")}, position);");
		}

		private void EmitZeroOrMore(CSharpWriter writer, PegOperation operation)
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
			writer.WriteLine($"return new PegResult<{TypeZeroOrMore(operation, false)}>(instances, position);");
			writer.EndBlock();
			writer.WriteBreak();
			writer.WriteLine("instances.Add(next.Value.Instance);");
			writer.WriteBreak();
			writer.WriteLine("position = next.Value.Position;");
			writer.EndBlock();
		}

		private void EmitZeroOrOne(CSharpWriter writer, PegOperation operation)
		{
			var index = operation.References[0].Index;
			var type = TypeZeroOrOne(operation);

			writer.WriteLine($"var one = {GetOperationName(index)}(stream, position);");
			writer.WriteBreak();
			writer.WriteLine("if (!one.HasValue)");
			writer.BeginBlock();
			writer.WriteLine($"return new PegResult<{type}>(new {type} {{ Defined = false }}, position);");
			writer.EndBlock();
			writer.WriteBreak();
			writer.WriteLine($"return new PegResult<{type}>(new {type} {{ Defined = true, Value = one.Value.Instance }}, one.Value.Position);");
		}

		private string TypeCharacterSet()
		{
			return "string";
		}

		private string TypeChoice(PegOperation operation)
		{
			var elements = operation.References
				.Where(reference => reference.Identifier is not null)
				.Select(reference => $"PegOption<{GetOperationType(reference.Index)}> {EscapeIdentifier(reference.Identifier!)}")
				.ToList();

			return elements.Count > 0 ? $"({string.Join(", ", elements)})" : "bool";
		}

		private string TypeOneOrMore(PegOperation operation, bool concrete)
		{
			return TypeZeroOrMore(operation, concrete);
		}

		private string TypeSequence(PegOperation operation)
		{
			var elements = operation.References
				.Where(reference => reference.Identifier is not null)
				.Select(reference => $"{GetOperationType(reference.Index)} {EscapeIdentifier(reference.Identifier!)}")
				.ToList();

			return elements.Count > 0 ? $"({string.Join(", ", elements)})" : "bool";
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
