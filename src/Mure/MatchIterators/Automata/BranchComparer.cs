using System.Collections.Generic;

namespace Mure.MatchIterators.Automata
{
	internal class BranchComparer : IComparer<Branch>
	{
		public static readonly BranchComparer Instance = new();

		public int Compare(Branch x, Branch y)
		{
			return x.Begin.CompareTo(y.Begin);
		}
	}
}
