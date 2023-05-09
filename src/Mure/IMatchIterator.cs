using System.Collections.Generic;

namespace Mure;

public interface IMatchIterator<TValue> : IEnumerable<Match<TValue>>
{
	public int Position { get; }

	bool TryMatchNext(out Match<TValue> match);
}
