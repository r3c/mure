using System;
using System.Collections.Generic;
using System.IO;

namespace Mure.Peg
{
	class PegGenerator
	{
		private static readonly IReadOnlyList<Action<PegWriter, PegOperation>> Emitters = new Action<PegWriter, PegOperation>[]
		{
			(writer, operation) => EmitCharacterSet(writer, operation),
			(writer, operation) => EmitChoice(writer, operation),
			(writer, operation) => EmitOneOrMore(writer, operation),
			(writer, operation) => EmitSequence(writer, operation),
			(writer, operation) => EmitZeroOrMore(writer, operation),
			(writer, operation) => EmitZeroOrOne(writer, operation)
		};

		public void Generate(TextWriter writer, IReadOnlyList<PegState> states, int startIndex)
		{
			writer.WriteLine(@"// Generated code

class PegStream
{
	private readonly System.Collections.Generic.List<int> _buffer;
	private readonly System.IO.TextReader _reader;

	public PegStream(System.IO.TextReader reader)
	{
		_buffer = new System.Collections.Generic.List<int>();
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
	public int? Parse(System.IO.TextReader reader)
	{
		var stream = new PegStream(reader);

		return " + GetOperationName(startIndex) + @"(stream, 0);
	}");

			var pegWriter = new PegWriter(writer, 1);

			for (var i = 0; i < states.Count; ++i)
			{
				var state = states[i];
				var operation = state.Operation;
				var emitter = Emitters[(int)operation.Operator];

				pegWriter.WriteBreak();
				pegWriter.WriteLine($"private int? {GetOperationName(i)}(PegStream stream, int position)");
				pegWriter.BeginBlock();

				emitter(pegWriter, operation);

				pegWriter.EndBlock();
			}

			writer.Write(@"
}");
		}

		private static string GetOperationName(int index)
		{
			return $"State{index}";
		}

		private static void EmitCharacterSet(PegWriter writer, PegOperation operation)
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
			writer.WriteBreak();
			writer.BeginBlock();
			writer.WriteLine("return position + 1;");
			writer.EndBlock();
			writer.WriteBreak();
			writer.WriteLine("return null;");
		}

		private static void EmitChoice(PegWriter writer, PegOperation operation)
		{
			foreach (int index in operation.StateIndices)
			{
				var name = $"choice{index}";

				writer.WriteLine($"var {name} = {GetOperationName(index)}(stream, position);");
				writer.WriteBreak();
				writer.WriteLine($"if ({name}.HasValue)");
				writer.BeginBlock();
				writer.WriteLine($"return {name};");
				writer.EndBlock();
				writer.WriteBreak();
			}

			writer.WriteLine("return null;");
		}

		private static void EmitOneOrMore(PegWriter writer, PegOperation operation)
		{
			var index = operation.StateIndices[0];

			writer.WriteLine($"var first = {GetOperationName(index)}(stream, position);");
			writer.WriteBreak();
			writer.WriteLine("if (!first.HasValue)");
			writer.BeginBlock();
			writer.WriteLine("return null;");
			writer.EndBlock();
			writer.WriteBreak();
			writer.WriteLine("position = first.Value;");
			writer.WriteBreak();

			EmitZeroOrMore(writer, operation);
		}

		private static void EmitSequence(PegWriter writer, PegOperation operation)
		{
			writer.WriteLine("int? next;");
			writer.WriteBreak();

			foreach (int index in operation.StateIndices)
			{
				writer.WriteLine($"next = {GetOperationName(index)}(stream, position);");
				writer.WriteBreak();
				writer.WriteLine($"if (!next.HasValue)");
				writer.BeginBlock();
				writer.WriteLine($"return null;");
				writer.EndBlock();
				writer.WriteBreak();
				writer.WriteLine($"position = next.Value;");
				writer.WriteBreak();
			}

			writer.WriteLine("return position;");
		}

		private static void EmitZeroOrMore(PegWriter writer, PegOperation operation)
		{
			var index = operation.StateIndices[0];

			writer.WriteLine("while (true)");
			writer.BeginBlock();
			writer.WriteLine($"var next = {GetOperationName(index)}(stream, position);");
			writer.WriteBreak();
			writer.WriteLine("if (!next.HasValue)");
			writer.BeginBlock();
			writer.WriteLine("return position;");
			writer.EndBlock();
			writer.WriteBreak();
			writer.WriteLine("position = next.Value;");
			writer.EndBlock();
		}

		private static void EmitZeroOrOne(PegWriter writer, PegOperation operation)
		{
			var index = operation.StateIndices[0];

			writer.WriteLine($"var next = {GetOperationName(index)}(stream, position);");
			writer.WriteBreak();
			writer.WriteLine("return next ?? position;");
		}
	}
}
