﻿using System;
using System.IO;
using Mure.Automata;
using Mure.Compilers.Pattern;
using Mure.Matchers;

namespace Mure.Compilers;

internal abstract class PatternCompiler<TValue> : ICompiler<string, TValue>
{
	private readonly NonDeterministicAutomata<TValue> _automata;
	private readonly IMatcher<Lexem> _patternLexer;
	private readonly int _start;

	protected PatternCompiler(IMatcher<Lexem> patternLexer)
	{
		var automata = new NonDeterministicAutomata<TValue>();

		_automata = automata;
		_patternLexer = patternLexer;
		_start = automata.PushEmpty();
	}

	public ICompiler<string, TValue> AddEndOfFile(TValue value)
	{
		var target = _automata.PushValue(value);

		_automata.BranchTo(_start, -1, -1, target);

		return this;
	}

	public ICompiler<string, TValue> AddPattern(string pattern, TValue value)
	{
		var node = ParsePattern(pattern);
		var stop = node.ConnectTo(_automata, _start);
		var tail = _automata.PushValue(value);

		_automata.EpsilonTo(stop, tail);

		return this;
	}

	public IMatcher<TValue> Compile()
	{
		var automata = _automata.ToDeterministic(_start);

		return automata.Error switch
		{
			ConversionError.Collision => throw new InvalidOperationException(
				$"transition collision between multiple values: {string.Join(", ", automata.Values)}"),
			ConversionError.None => new AutomataMatcher<TValue>(automata.Result),
			_ => throw new InvalidOperationException($"internal failure with unknown error '{automata.Error}'")
		};
	}

	/// <Summary>
	/// Compile regular pattern into graph of non-deterministic states leading to given value.
	/// </Summary>
	protected abstract Node CreateGraph(IMatchIterator<Lexem> iterator);

	private Node ParsePattern(string pattern)
	{
		using var reader = new StringReader(pattern);

		var iterator = _patternLexer.Open(reader);

		return CreateGraph(iterator);
	}
}
