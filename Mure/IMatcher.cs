
namespace Mure
{
	public interface IMatcher<TValue>
	{
		bool TryMatch(out Match<TValue> match);
	}
}