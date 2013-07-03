using System.Collections.Generic;
using System.Linq;

namespace LazerTagHostLibrary
{
	public class ScoredObject
	{
		public int Score { get; set; }
		public int Rank { get; set; }

		public static void CalculateRanks(IEnumerable<ScoredObject> scoredObjects)
		{
			var items = scoredObjects as ScoredObject[] ?? scoredObjects.ToArray();
			var rankings = LazerTagHostLibrary.Rank.Calculate(items);
			foreach (var item in items)
			{
				item.Rank = rankings[item];
			}
		}

		public int CompareScoreTo(ScoredObject other)
		{
			return Score.CompareTo(other.Score);
		}
	}

	public class ScoreComparer : IComparer<ScoredObject>
	{
		public int Compare(ScoredObject x, ScoredObject y)
		{
			return x.CompareScoreTo(y);
		}
	}
}
