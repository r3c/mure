using System;
using System.Collections.Generic;

namespace Mure.Automata;

internal record struct ConversionResult<TResult, TValue>(ConversionError Error, TResult Result,
	IReadOnlyList<TValue> Values)
{
	public static ConversionResult<TResult, TValue> Collision(IReadOnlyList<TValue> values)
	{
		return new ConversionResult<TResult, TValue>(ConversionError.Collision, default!, values);
	}

	public static ConversionResult<TResult, TValue> Success(TResult result)
	{
		return new ConversionResult<TResult, TValue>(ConversionError.None, result, Array.Empty<TValue>());
	}
}
