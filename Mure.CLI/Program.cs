using System;
using System.Collections.Generic;
using System.Linq;

namespace Mure.CLI
{
	class Program
	{
		static void Main(string[] args)
		{
			var scanner = ScannerFactory.CreateRegex(new[]
			{
				("ab*c", true)
			});

			var matcher = scanner.Scan(Console.In);

			while (matcher.TryMatch(out var match))
				Console.WriteLine($"{match.Capture}: {match.Value}");
		}
	}
}
