using System.IO;

namespace Mure.Peg
{
	public interface IGenerator
	{
		void Generate(TextWriter writer);
	}
}
