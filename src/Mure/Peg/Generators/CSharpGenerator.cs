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
					.Select(reference => new CSharpSymbol(GetTypeOptionOf(generator.GetOperationType(reference.Key)), CSharpSymbol.SanitizeIdentifier(reference.Identifier!)))
					.ToList()),
				(generator, writer, context, operation, returnType, converterBody) => generator.EmitStateChoice(writer, context, operation, returnType, converterBody)
			),
			new CSharpImplementation(
				(generator, operation) => GetTypeListOf(generator.GetOperationType(operation.References[0].Key)),
				(generator, writer, context, operation, returnType, converterBody) => generator.EmitStateOneOrMore(writer, context, operation, returnType, converterBody)
			),
			new CSharpImplementation(
				(generator, operation) => GetTypeTupleOf(operation.References
					.Where(reference => reference.Identifier is not null)
					.Select(reference => new CSharpSymbol(generator.GetOperationType(reference.Key), CSharpSymbol.SanitizeIdentifier(reference.Identifier!)))
					.ToList()),
				(generator, writer, context, operation, returnType, converterBody) => generator.EmitStateSequence(writer, context, operation, returnType, converterBody)
			),
			new CSharpImplementation(
				(generator, operation) => GetTypeListOf(generator.GetOperationType(operation.References[0].Key)),
				(generator, writer, context, operation, returnType, converterBody) => generator.EmitStateZeroOrMore(writer, context, operation, returnType, converterBody)
			),
			new CSharpImplementation(
				(generator, operation) => GetTypeOptionOf(generator.GetOperationType(operation.References[0].Key)),
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

		protected override PegError? EmitFooter(CSharpWriter writer)
		{
			writer.EndBlock();

			return null;
		}

		protected override PegError? EmitHeader(CSharpWriter writer, string contextType, string startKey)
		{
			writer.WriteLine(@"// Generated code
using System;
using System.Collections.Generic;
using System.IO;

readonly struct PegOption<T>
{
	public static readonly PegOption<T> Empty = new PegOption<T>(false, default!);

	public static PegOption<T> Create(T value)
	{
		return new PegOption<T>(true, value);
	}

	public readonly bool Defined;
	public readonly T Value;

	private PegOption(bool defined, T value)
	{
		Defined = defined;
		Value = value;
	}
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

			writer.WriteLine(@"public PegOption<" + GetOperationType(startKey) + @"> Parse(TextReader reader, " + contextType + @" context)
	{
		var stream = new PegStream(reader);
		var result = " + GetOperationName(startKey) + @"(stream, context, 0);

		if (!result.HasValue)
			return " + GetCreateOptionEmpty(GetOperationType(startKey)) + @";

		return " + GetCreateOptionValue(GetOperationType(startKey), "result.Value.Instance") + @";
	}");

			return null;
		}

		protected override PegError? EmitState(CSharpWriter writer, string contextType, string startKey)
		{
			if (!TryGetState(startKey, out var operation, out var action))
				return PegError.CreateUnknownStateKey(startKey);

			var context = new CSharpSymbol(contextType, "context");
			var implementation = Implementations[(int)operation.Operator];
			var returnType = GetOperationType(startKey);

			writer.WriteBreak();
			writer.WriteLine($"private PegResult<{returnType}>? {GetOperationName(startKey)}(PegStream stream, {context.FormatDeclaration()}, int position)");
			writer.BeginBlock();

			var error = implementation.Write(this, writer, context, operation, action.HasValue ? action.Value.Type : returnType, action.HasValue ? $"{{ {action.Value.Body} }}" : null);

			if (error is not null)
				return error;

			writer.EndBlock();

			return null;
		}

		private static string GetCreateConverter(IReadOnlyList<CSharpSymbol> arguments, string returnType, string converterBody)
		{
			return $"new Func<{string.Join(", ", arguments.Select(namedType => namedType.Type).Append(returnType))}>(({string.Join(", ", arguments.Select(namedType => $"{namedType.Type} {namedType.Identifier}"))}) => {converterBody})";
		}

		private static string GetCreateList(string type)
		{
			return $"new List<{type}>()";
		}

		private static string GetCreateOptionEmpty(string type)
		{
			return $"PegOption<{type}>.Empty";
		}

		private static string GetCreateOptionValue(string type, string value)
		{
			return $"PegOption<{type}>.Create({value})";
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
				return $"({fields[0].FormatDeclaration()}, bool _)";

			return $"({string.Join(", ", fields.Select(element => element.FormatDeclaration()))})";
		}

		private static string GetOperationName(string stateKey)
		{
			return $"State{CSharpSymbol.SanitizeIdentifier(stateKey)}";
		}

		private string GetOperationType(string stateKey)
		{
			if (!TryGetState(stateKey, out var operation, out var action))
				return "void";

			if (action.HasValue)
				return action.Value.Type;

			var implementation = Implementations[(int)operation.Operator];

			return implementation.Infer(this, operation);
		}

		private PegError? EmitStateCharacterSet(CSharpWriter writer, CSharpSymbol context, PegOperation operation, string returnType, string? converterBody)
		{
			var capture = new CSharpSymbol("string", "capture");
			var buffer = new StringBuilder();
			var next = string.Empty;

			foreach (var range in operation.CharacterRanges)
			{
				buffer.Append($"{next}(character >= {(int)range.Begin} && character <= {(int)range.End})");

				next = " || ";
			}

			writer.WriteLine($"var converter = {GetCreateConverter(new[] { context, capture }, returnType, converterBody ?? capture.Identifier)};");
			writer.WriteLine("var character = stream.ReadAt(position);");
			writer.WriteBreak();
			writer.WriteLine($"if ({buffer})");
			writer.BeginBlock();
			writer.WriteLine($"return new PegResult<{returnType}>(converter({context.Identifier}, new string((char)character, 1)), position + 1);");
			writer.EndBlock();
			writer.WriteBreak();
			writer.WriteLine("return null;");

			return null;
		}

		private PegError? EmitStateChoice(CSharpWriter writer, CSharpSymbol context, PegOperation operation, string returnType, string? converterBody)
		{
			var references = operation.References;

			var defaultValues = references
				.Select((reference, index) => new
				{
					Captured = reference.Identifier is not null,
					Order = index,
					Symbol = GetCreateOptionEmpty(GetOperationType(reference.Key))
				})
				.Where(tuple => tuple.Captured)
				.ToList();

			var choices = references
				.Where(reference => reference.Identifier is not null)
				.Select(reference => new CSharpSymbol(GetTypeOptionOf(GetOperationType(reference.Key)), CSharpSymbol.SanitizeIdentifier(reference.Identifier!)))
				.Prepend(context)
				.ToList();

			writer.WriteLine($"var converter = {GetCreateConverter(choices, returnType, converterBody ?? GetCreateTuple(choices.Select(namedType => namedType.Identifier).ToList()))};");
			writer.WriteBreak();

			var i = 0;

			foreach (var reference in references)
			{
				var result = CSharpSymbol.SanitizeIdentifier($"result{i}");
				var values = defaultValues.Select(tuple => tuple.Order == i ? GetCreateOptionValue(GetOperationType(reference.Key), $"{result}.Value.Instance") : tuple.Symbol);

				writer.WriteLine($"var {result} = {GetOperationName(reference.Key)}(stream, {context.Identifier}, position);");
				writer.WriteBreak();
				writer.WriteLine($"if ({result}.HasValue)");
				writer.BeginBlock();
				writer.WriteLine($"return new PegResult<{returnType}>(converter({context.Identifier}, {string.Join(", ", values)}), {result}.Value.Position);");
				writer.EndBlock();
				writer.WriteBreak();

				++i;
			}

			writer.WriteLine("return null;");

			return null;
		}

		private PegError? EmitStateOneOrMore(CSharpWriter writer, CSharpSymbol context, PegOperation operation, string returnType, string? converterBody)
		{
			var reference = operation.References[0];
			var matchType = GetOperationType(reference.Key);
			var sequence = new CSharpSymbol(GetTypeListOf(matchType), CSharpSymbol.SanitizeIdentifier(reference.Identifier ?? "elements"));

			writer.WriteLine($"var converter = {GetCreateConverter(new[] { context, sequence }, returnType, converterBody ?? sequence.Identifier)};");
			writer.WriteLine($"var first = {GetOperationName(reference.Key)}(stream, {context.Identifier}, position);");
			writer.WriteBreak();
			writer.WriteLine("if (!first.HasValue)");
			writer.BeginBlock();
			writer.WriteLine("return null;");
			writer.EndBlock();
			writer.WriteBreak();
			writer.WriteLine($"var instances = {GetCreateList(GetOperationType(reference.Key))};");
			writer.WriteBreak();
			writer.WriteLine("instances.Add(first.Value.Instance);");
			writer.WriteBreak();
			writer.WriteLine("position = first.Value.Position;");
			writer.WriteBreak();
			writer.WriteLine("while (true)"); // FIXME: similar to EmitZeroOrMore
			writer.BeginBlock();
			writer.WriteLine($"var next = {GetOperationName(reference.Key)}(stream, {context.Identifier}, position);");
			writer.WriteBreak();
			writer.WriteLine("if (!next.HasValue)");
			writer.BeginBlock();
			writer.WriteLine($"return new PegResult<{returnType}>(converter({context.Identifier}, instances), position);");
			writer.EndBlock();
			writer.WriteBreak();
			writer.WriteLine("instances.Add(next.Value.Instance);");
			writer.WriteBreak();
			writer.WriteLine("position = next.Value.Position;");
			writer.EndBlock();

			return null;
		}

		private PegError? EmitStateSequence(CSharpWriter writer, CSharpSymbol context, PegOperation operation, string returnType, string? converterBody)
		{
			var references = operation.References;

			var elements = references
				.Where(reference => reference.Identifier is not null)
				.Select(reference => new CSharpSymbol(GetOperationType(reference.Key), CSharpSymbol.SanitizeIdentifier(reference.Identifier!)))
				.Prepend(context)
				.ToList();

			var fragments = references
				.Select((reference, index) => new
				{
					Captured = reference.Identifier is not null,
					reference.Key,
					Symbol = $"fragment{index++}"
				})
				.ToList();

			writer.WriteLine($"var converter = {GetCreateConverter(elements, returnType, converterBody ?? GetCreateTuple(elements.Select(namedType => namedType.Identifier).ToList()))};");

			foreach (var fragment in fragments)
			{
				writer.WriteLine($"var {fragment.Symbol} = {GetOperationName(fragment.Key)}(stream, {context.Identifier}, position);");
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

			writer.WriteLine($"return new PegResult<{returnType}>(converter({context.Identifier}, {string.Join(", ", values)}), position);");

			return null;
		}

		private PegError? EmitStateZeroOrMore(CSharpWriter writer, CSharpSymbol context, PegOperation operation, string returnType, string? converterBody)
		{
			var reference = operation.References[0];
			var matchType = GetTypeListOf(GetOperationType(reference.Key));
			var sequence = new CSharpSymbol(matchType, CSharpSymbol.SanitizeIdentifier(reference.Identifier ?? "elements"));

			writer.WriteLine($"var converter = {GetCreateConverter(new[] { context, sequence }, returnType, converterBody ?? sequence.Identifier)};");
			writer.WriteLine($"var instances = {GetCreateList(GetOperationType(reference.Key))};");
			writer.WriteBreak();
			writer.WriteLine("while (true)");
			writer.BeginBlock();
			writer.WriteLine($"var next = {GetOperationName(reference.Key)}(stream, {context.Identifier}, position);");
			writer.WriteBreak();
			writer.WriteLine("if (!next.HasValue)");
			writer.BeginBlock();
			writer.WriteLine($"return new PegResult<{returnType}>(converter({context.Identifier}, instances), position);");
			writer.EndBlock();
			writer.WriteBreak();
			writer.WriteLine("instances.Add(next.Value.Instance);");
			writer.WriteBreak();
			writer.WriteLine("position = next.Value.Position;");
			writer.EndBlock();

			return null;
		}

		private PegError? EmitStateZeroOrOne(CSharpWriter writer, CSharpSymbol context, PegOperation operation, string returnType, string? converterBody)
		{
			var reference = operation.References[0];
			var matchType = GetOperationType(reference.Key);
			var option = new CSharpSymbol(GetTypeOptionOf(matchType), CSharpSymbol.SanitizeIdentifier(reference.Identifier ?? "option"));

			writer.WriteLine($"var converter = {GetCreateConverter(new[] { context, option }, returnType, converterBody ?? option.Identifier)};");
			writer.WriteLine($"var one = {GetOperationName(reference.Key)}(stream, {context.Identifier}, position);");
			writer.WriteLine($"{option.Type} instance;");
			writer.WriteBreak();
			writer.WriteLine("if (one.HasValue)");
			writer.BeginBlock();
			writer.WriteLine($"instance = {GetCreateOptionValue(matchType, "one.Value.Instance")};");
			writer.WriteLine($"position = one.Value.Position;");
			writer.EndBlock();
			writer.WriteLine("else");
			writer.BeginBlock();
			writer.WriteLine($"instance = {GetCreateOptionEmpty(matchType)};");
			writer.EndBlock();
			writer.WriteBreak();
			writer.WriteLine($"return new PegResult<{returnType}>(converter({context.Identifier}, instance), position);");

			return null;
		}
	}
}
