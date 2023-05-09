namespace Mure;

public interface IMatchIterator<TValue>
{
	public int Position { get; }

	bool TryMatchNext(out Match<TValue> match);
}
