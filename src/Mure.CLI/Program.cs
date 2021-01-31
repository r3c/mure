using System;

namespace Mure.CLI
{
	class Program
	{
		static void Main(string[] args)
		{
			var matcher = Matcher.CreateFromRegex(new[]
			{
				("ab*c", true)
			});

			var iterator = matcher.Open(Console.In);

			while (iterator.TryMatchNext(out var match))
				Console.WriteLine($"{match.Capture}: {match.Value}");
		}
	}
}
