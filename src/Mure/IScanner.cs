using System.Collections.Generic;
using System.IO;

namespace Mure
{
	public interface IScanner<TValue>
	{
		IMatcher<TValue> Scan(TextReader reader);
	}
}
