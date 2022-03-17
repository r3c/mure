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

		private static readonly IReadOnlyList<CSharpImplementation> Implementations = new[]
		{
			new CSharpImplementation(
				(generator, operation) => "string",
				(generator, writer, context, operation, returnType, converterBody) => generator.EmitStateCharacterSet(writer, context, operation, returnType, converterBody)
			),
			new CSharpImplementation(
				(generator, operation) => GetTypeTupleOf(operation.References
					.Where(reference => reference.Identifier is not null)
					.Select(reference => new CSharpSymbol(GetTypeOptionOf(generator.GetOperationType(reference.Index)), reference.Identifier!))
					.ToList()),
				(generator, writer, context, operation, returnType, converterBody) => generator.EmitStateChoice(writer, context, operation, returnType, converterBody)
			),
			new CSharpImplementation(
				(generator, operation) => GetTypeListOf(generator.GetOperationType(operation.References[0].Index)),
				(generator, writer, context, operation, returnType, converterBody) => generator.EmitStateOneOrMore(writer, context, operation, returnType, converterBody)
			),
			new CSharpImplementation(
				(generator, operation) => GetTypeTupleOf(operation.References
					.Where(reference => reference.Identifier is not null)
					.Select(reference => new CSharpSymbol(generator.GetOperationType(reference.Index), reference.Identifier!))
					.ToList()),
				(generator, writer, context, operation, returnType, converterBody) => generator.EmitStateSequence(writer, context, operation, returnType, converterBody)
			),
			new CSharpImplementation(
				(generator, operation) => GetTypeListOf(generator.GetOperationType(operation.References[0].Index)),
				(generator, writer, context, operation, returnType, converterBody) => generator.EmitStateZeroOrMore(writer, context, operation, returnType, converterBody)
			),
			new CSharpImplementation(
				(generator, operation) => GetTypeOptionOf(generator.GetOperationType(operation.References[0].Index)),
				(generator, writer, context, operation, returnType, converterBody) => generator.EmitStateZeroOrOne(writer, context, operation, returnType, converterBody)
			)
		};

		public CSharpGenerator(PegDefinition definition) :
			base(LanguageName, definition)
		{
		}

		protected override CSharpWriter CreateContext(TextWriter writer)
		{
			return new CSharpWriter(writer);
		}

		protected override void EmitFooter(CSharpWriter writer)
		{
			writer.EndBlock();
		}

		protected override void EmitHeader(CSharpWriter writer, string contextType, int startIndex)
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

class Parser");

			writer.BeginBlock();

			writer.WriteLine(@"public PegOption<" + GetOperationType(startIndex) + @"> Parse(TextReader reader, " + EscapeIdentifier(contextType) + @" context)
	{
		var stream = new PegStream(reader);
		var result = " + GetOperationName(startIndex) + @"(stream, context, 0);

		if (!result.HasValue)
			return " + GetCreateOptionEmpty(GetOperationType(startIndex)) + @";

		return " + GetCreateOptionValue(GetOperationType(startIndex), "result.Value.Instance") + @";
	}");
		}

		protected override void EmitState(CSharpWriter writer, string contextType, int stateIndex)
		{
			var (operation, action) = GetState(stateIndex);
			var context = new CSharpSymbol(contextType, "context");
			var implementation = Implementations[(int)operation.Operator];
			var returnType = GetOperationType(stateIndex);

			writer.WriteBreak();
			writer.WriteLine($"private PegResult<{returnType}>? {GetOperationName(stateIndex)}(PegStream stream, {EscapeIdentifier(context.Type)} {EscapeIdentifier(context.Identifier)}, int position)");
			writer.BeginBlock();

			implementation.Write(this, writer, context, operation, action.HasValue ? action.Value.Type : returnType, action.HasValue ? $"{{ {action.Value.Body} }}" : null);

			writer.EndBlock();
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

		private static string GetDeclareConverter(IReadOnlyList<CSharpSymbol> arguments, string returnType, string converterBody)
		{
			return $"new Func<{string.Join(", ", arguments.Select(namedType => namedType.Type).Append(returnType))}>(({string.Join(", ", arguments.Select(namedType => $"{namedType.Type} {namedType.Identifier}"))}) => {converterBody})";
		}

		private static string GetTypeListOf(string type)
		{
			return $"IReadOnlyList<{type}>";
		}

		private static string GetTypeOptionOf(string type)
		{
			return $"PegOption<{type}>";
		}

		private static string GetTypeTupleOf(IReadOnlyList<CSharpSymbol> fields)
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

			var implementation = Implementations[(int)operation.Operator];

			return implementation.Infer(this, operation);
		}

		private void EmitStateCharacterSet(CSharpWriter writer, CSharpSymbol context, PegOperation operation, string returnType, string? converterBody)
		{
			var capture = new CSharpSymbol("string", "capture");
			var buffer = new StringBuilder();
			var next = string.Empty;

			foreach (var range in operation.CharacterRanges)
			{
				buffer.Append($"{next}(character >= {(int)range.Begin} && character <= {(int)range.End})");

				next = " || ";
			}

			writer.WriteLine($"var converter = {GetDeclareConverter(new[] { context, capture }, returnType, converterBody ?? EscapeIdentifier(capture.Identifier))};");
			writer.WriteLine("var character = stream.ReadAt(position);");
			writer.WriteBreak();
			writer.WriteLine($"if ({buffer})");
			writer.BeginBlock();
			writer.WriteLine($"return new PegResult<{EscapeIdentifier(returnType)}>(converter({EscapeIdentifier(context.Identifier)}, new string((char)character, 1)), position + 1);");
			writer.EndBlock();
			writer.WriteBreak();
			writer.WriteLine("return null;");
		}

		private void EmitStateChoice(CSharpWriter writer, CSharpSymbol context, PegOperation operation, string returnType, string? converterBody)
		{
			var references = operation.References;

			var defaultValues = references
				.Select((reference, index) => new
				{
					Captured = reference.Identifier is not null,
					Order = index,
					Symbol = GetCreateOptionEmpty(GetOperationType(reference.Index))
				})
				.Where(tuple => tuple.Captured)
				.ToList();

			var choices = references
				.Where(reference => reference.Identifier is not null)
				.Select(reference => new CSharpSymbol(GetTypeOptionOf(GetOperationType(reference.Index)), reference.Identifier!))
				.Prepend(context)
				.ToList();

			writer.WriteLine($"var converter = {GetDeclareConverter(choices, returnType, converterBody ?? GetCreateTuple(choices.Select(namedType => namedType.Identifier).ToList()))};");
			writer.WriteBreak();

			var i = 0;

			foreach (var reference in references)
			{
				var result = EscapeIdentifier($"result{i}");
				var values = defaultValues.Select(tuple => tuple.Order == i ? GetCreateOptionValue(GetOperationType(reference.Index), $"{result}.Value.Instance") : tuple.Symbol);

				writer.WriteLine($"var {result} = {GetOperationName(reference.Index)}(stream, {EscapeIdentifier(context.Identifier)}, position);");
				writer.WriteBreak();
				writer.WriteLine($"if ({result}.HasValue)");
				writer.BeginBlock();
				writer.WriteLine($"return new PegResult<{EscapeIdentifier(returnType)}>(converter({EscapeIdentifier(context.Identifier)}, {string.Join(", ", values)}), {result}.Value.Position);");
				writer.EndBlock();
				writer.WriteBreak();

				++i;
			}

			writer.WriteLine("return null;");
		}

		private void EmitStateOneOrMore(CSharpWriter writer, CSharpSymbol context, PegOperation operation, string returnType, string? converterBody)
		{
			var reference = operation.References[0];
			var matchType = GetTypeListOf(GetOperationType(reference.Index));
			var sequence = new CSharpSymbol(matchType, reference.Identifier ?? "elements");

			writer.WriteLine($"var converter = {GetDeclareConverter(new[] { context, sequence }, returnType, converterBody ?? sequence.Identifier)};");
			writer.WriteLine($"var first = {GetOperationName(reference.Index)}(stream, {EscapeIdentifier(context.Identifier)}, position);");
			writer.WriteBreak();
			writer.WriteLine("if (!first.HasValue)");
			writer.BeginBlock();
			writer.WriteLine("return null;");
			writer.EndBlock();
			writer.WriteBreak();
			writer.WriteLine($"var instances = {GetCreateList(GetOperationType(reference.Index))};");
			writer.WriteBreak();
			writer.WriteLine("instances.Add(first.Value.Instance);");
			writer.WriteBreak();
			writer.WriteLine("position = first.Value.Position;");
			writer.WriteBreak();
			writer.WriteLine("while (true)"); // FIXME: similar to EmitZeroOrMore
			writer.BeginBlock();
			writer.WriteLine($"var next = {GetOperationName(reference.Index)}(stream, {EscapeIdentifier(context.Identifier)}, position);");
			writer.WriteBreak();
			writer.WriteLine("if (!next.HasValue)");
			writer.BeginBlock();
			writer.WriteLine($"return new PegResult<{EscapeIdentifier(returnType)}>(converter({EscapeIdentifier(context.Identifier)}, instances), position);");
			writer.EndBlock();
			writer.WriteBreak();
			writer.WriteLine("instances.Add(next.Value.Instance);");
			writer.WriteBreak();
			writer.WriteLine("position = next.Value.Position;");
			writer.EndBlock();
		}

		private void EmitStateSequence(CSharpWriter writer, CSharpSymbol context, PegOperation operation, string returnType, string? converterBody)
		{
			var references = operation.References;

			var elements = references
				.Where(reference => reference.Identifier is not null)
				.Select(reference => new CSharpSymbol(GetOperationType(reference.Index), reference.Identifier!))
				.Prepend(context)
				.ToList();

			var fragments = references
				.Select((reference, index) => new
				{
					Captured = reference.Identifier is not null,
					reference.Index,
					Symbol = $"fragment{index++}"
				})
				.ToList();

			writer.WriteLine($"var converter = {GetDeclareConverter(elements, returnType, converterBody ?? GetCreateTuple(elements.Select(namedType => namedType.Identifier).ToList()))};");

			foreach (var fragment in fragments)
			{
				writer.WriteLine($"var {fragment.Symbol} = {GetOperationName(fragment.Index)}(stream, {EscapeIdentifier(context.Identifier)}, position);");
				writer.WriteBreak();
				writer.WriteLine($"if (!{fragment.Symbol}.HasValue)");
				writer.BeginBlock();
				writer.WriteLine($"return null;");
				writer.EndBlock();
				writer.WriteBreak();
				writer.WriteLine($"position = {fragment.Symbol}.Value.Position;");
				writer.WriteBreak();
			}

			var values = fragments.Where(fragment => fragment.Captured).Select(fragment => $"{fragment.Symbol}.Value.Instance");

			writer.WriteLine($"return new PegResult<{EscapeIdentifier(returnType)}>(converter({EscapeIdentifier(context.Identifier)}, {string.Join(", ", values)}), position);");
		}

		private void EmitStateZeroOrMore(CSharpWriter writer, CSharpSymbol context, PegOperation operation, string returnType, string? converterBody)
		{
			var reference = operation.References[0];
			var matchType = GetTypeListOf(GetOperationType(reference.Index));
			var sequence = new CSharpSymbol(matchType, reference.Identifier ?? "elements");

			writer.WriteLine($"var converter = {GetDeclareConverter(new[] { context, sequence }, returnType, converterBody ?? sequence.Identifier)};");
			writer.WriteLine($"var instances = {GetCreateList(GetOperationType(reference.Index))};");
			writer.WriteBreak();
			writer.WriteLine("while (true)");
			writer.BeginBlock();
			writer.WriteLine($"var next = {GetOperationName(reference.Index)}(stream, {EscapeIdentifier(context.Identifier)}, position);");
			writer.WriteBreak();
			writer.WriteLine("if (!next.HasValue)");
			writer.BeginBlock();
			writer.WriteLine($"return new PegResult<{EscapeIdentifier(returnType)}>(converter({EscapeIdentifier(context.Identifier)}, instances), position);");
			writer.EndBlock();
			writer.WriteBreak();
			writer.WriteLine("instances.Add(next.Value.Instance);");
			writer.WriteBreak();
			writer.WriteLine("position = next.Value.Position;");
			writer.EndBlock();
		}

		private void EmitStateZeroOrOne(CSharpWriter writer, CSharpSymbol context, PegOperation operation, string returnType, string? converterBody)
		{
			var reference = operation.References[0];
			var matchType = GetTypeOptionOf(GetOperationType(reference.Index));
			var option = new CSharpSymbol(matchType, reference.Identifier ?? "option");

			writer.WriteLine($"var converter = {GetDeclareConverter(new[] { context, option }, returnType, converterBody ?? option.Identifier)};");
			writer.WriteLine($"var one = {GetOperationName(reference.Index)}(stream, {EscapeIdentifier(context.Identifier)}, position);");
			writer.WriteLine($"{matchType} instance;");
			writer.WriteBreak();
			writer.WriteLine("if (one.HasValue)");
			writer.BeginBlock();
			writer.WriteLine($"instance = new {matchType} {{ Defined = true, Value = one.Value.Instance }};");
			writer.WriteLine($"position = one.Value.Position;");
			writer.EndBlock();
			writer.WriteLine("else");
			writer.BeginBlock();
			writer.WriteLine($"instance = new {matchType} {{ Defined = false }};");
			writer.EndBlock();
			writer.WriteBreak();
			writer.WriteLine($"return new PegResult<{EscapeIdentifier(returnType)}>(converter({EscapeIdentifier(context.Identifier)}, instance), position);");
		}
	}
}
