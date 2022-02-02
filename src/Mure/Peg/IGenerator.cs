using System.IO;

namespace Mure.Peg
{
	interface IGenerator
	{
		void Generate(TextWriter writer, int startIndex);
	}
}
