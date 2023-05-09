using System.IO;

namespace Mure;

public interface IMatcher<TValue>
{
	IMatchIterator<TValue> Open(TextReader reader);
}
