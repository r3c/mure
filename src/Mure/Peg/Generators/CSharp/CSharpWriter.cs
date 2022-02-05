using System.IO;

namespace Mure.Peg
{
	class CSharpWriter
	{
		private readonly TextWriter _writer;

		private int _indent;

		public CSharpWriter(TextWriter writer)
		{
			_indent = 0;
			_writer = writer;
		}

		public void BeginBlock()
		{
			Indent();

			_writer.WriteLine("{");

			++_indent;
		}

		public void EndBlock()
		{
			--_indent;

			Indent();

			_writer.WriteLine("}");
		}

		public void WriteBreak()
		{
			_writer.WriteLine();
		}

		public void WriteLine(string line)
		{
			Indent();

			_writer.WriteLine(line);
		}

		private void Indent()
		{
			for (var i = 0; i < _indent; ++i)
				_writer.Write("    ");
		}
	}
}
