using System;
using System.Collections.Generic;
using System.Linq;

namespace LazerTagHostLibrary
{
	public class Rank
	{
		public static IDictionary<ScoredObject, int> Calculate(IEnumerable<ScoredObject> items, RankingStrategy strategy = RankingStrategy.Competition)
		{
			if (strategy != RankingStrategy.Competition && strategy != RankingStrategy.Ordinal)
				throw new NotImplementedException(string.Format("The RankingStrategy, {0}, is not implemented.", strategy));

			var rankings = new Dictionary<ScoredObject, int>();
			var sortedItems = items.OrderByDescending(i => i, new ScoreComparer());
			var rank = 1;
			ScoredObject previousItem = null;
			foreach (var item in sortedItems)
			{
				if (previousItem != null && strategy == RankingStrategy.Competition && item.Score == previousItem.Score)
				{
					rankings[item] = rankings[previousItem];
				}
				else
				{
					rankings[item] = rank;
				}
				rank++;
				previousItem = item;
			}
			return rankings;
		}

	}

	// See http://en.wikipedia.org/wiki/Ranking
	public enum RankingStrategy
	{
		Competition,			// 1224
		ModifiedCompetition,	// 1334
		Dense,					// 1223
		Ordinal,				// 1234
		Fractional,				// 1, 2.5, 2.5, 4
	}
}
