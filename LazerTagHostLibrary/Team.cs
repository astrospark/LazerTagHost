using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace LazerTagHostLibrary
{
	public class Team : ScoredObject
	{
		public Team(int teamNumber)
		{
			Number = teamNumber;
		}

		private readonly PlayerCollection _players = new PlayerCollection();
		public PlayerCollection Players
		{
			get { return _players; }
		}

		public int Number { get; set; }
	}

	public class TeamCollection: ICollection<Team>
	{
		private readonly List<Team> _teams = new List<Team>();

		public void CalculateRanks()
		{
			ScoredObject.CalculateRanks(this);
		}

		public Team Team(int teamNumber)
		{
			try
			{
				return _teams.FirstOrDefault(team => team.Number == teamNumber);
			}
			catch (InvalidOperationException)
			{
				return null;
			}
		}

		public IEnumerator<Team> GetEnumerator()
		{
			return _teams.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public void Add(Team item)
		{
			_teams.Add(item);
		}

		public void Clear()
		{
			_teams.Clear();
		}

		public bool Contains(Team item)
		{
			return (_teams.Contains(item));
		}

		public void CopyTo(Team[] array, int arrayIndex)
		{
			_teams.CopyTo(array, arrayIndex);
		}

		public bool Remove(Team item)
		{
			return _teams.Remove(item);
		}

		public int Count
		{
			get { return _teams.Count; }
		}

		public bool IsReadOnly
		{
			get { return false; }
		}
	}
}
