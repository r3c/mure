using System.IO;

namespace Mure.Peg
{
	public interface IGenerator
	{
		PegError? Generate(TextWriter writer);
	}
}
