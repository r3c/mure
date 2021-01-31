
namespace Mure
{
    public interface IMatcher<TValue>
    {
        bool TryMatchNext(out Match<TValue> match);
    }
}