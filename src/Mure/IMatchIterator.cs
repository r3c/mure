namespace Mure
{
	public interface IMatchIterator<TValue>
	{
		bool TryMatchNext(out Match<TValue> match);
	}
}
