using System.IO;

namespace Mure.Peg
{
	class PegWriter
	{
		private readonly TextWriter _writer;

		private int _indent;

		public PegWriter(TextWriter writer, int indent)
		{
			_indent = indent;
			_writer = writer;
		}

		public void BeginBlock()
		{
			_writer.WriteLine("{");

			++_indent;
		}

		public void EndBlock()
		{
			--_indent;

			_writer.WriteLine("}");
		}

		public void WriteBreak()
		{
			_writer.WriteLine();
		}

		public void WriteLine(string line)
		{
			for (var i = 0; i < _indent; ++i)
				_writer.Write("    ");

			_writer.WriteLine(line);
		}
	}
}
