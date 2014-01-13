using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace LazerTagHostLibrary
{
	public class Player : ScoredObject
	{
		private readonly HostGun _hostGun;
		public byte TaggerId { get; set; }
		public bool Confirmed = false;
		public bool Dropped = false;
		public bool TagSummaryReceived = false;
		public TeamPlayerId TeamPlayerId { get; set; }
		public Team Team { get; set; }

		public int TagsTaken = 0;
		public TimeSpan SurviveTime { get; set; }
		public bool Survived { get; set; }
		public bool[] TeamTagReportsExpected = {false, false, false};
		public bool[] TeamTagReportsReceived = {false, false, false};

		private int[] _taggedPlayerCounts = new int[TeamPlayerId.MaximumPlayerNumber];
		public int[] TaggedPlayerCounts
		{
			get { return _taggedPlayerCounts; }
			set { _taggedPlayerCounts = value; }
		}

		private int[] _taggedByPlayerCounts = new int[TeamPlayerId.MaximumPlayerNumber];
		public int[] TaggedByPlayerCounts
		{
			get { return _taggedByPlayerCounts; }
			set { _taggedByPlayerCounts = value; }
		}

		public TimeSpan ZoneTime;

		// TODO: Remove this and handle it in the UI layer
		public string Name { get; set; }

		public string DisplayName
		{
			get
			{
				var name = string.IsNullOrWhiteSpace(Name) ? "Player" : Name;
				var playerId = TeamPlayerId.ToString(_hostGun.GameDefinition.IsTeamGame);
				return string.Format("{0} ({1})", name, playerId);
			}
		}

		public Player(HostGun hostGun, byte taggerId)
		{
			_hostGun = hostGun;
			TaggerId = taggerId;
			Survived = false;
		}

		public bool AllTagReportsReceived()
		{
			if (Dropped) return true;
			if (!TagSummaryReceived) return false;

			for (var i = 0; i < 3; i++)
			{
				if (TeamTagReportsExpected[i] && !TeamTagReportsReceived[i]) return false;
			}

			return true;
		}
	}

	public class PlayerCollection : ICollection<Player>
	{
		private readonly List<Player> _players = new List<Player>();

		public void CalculateRanks()
		{
			ScoredObject.CalculateRanks(this);
		}

		public Player Player(TeamPlayerId teamPlayerId)
		{
			try
			{
				return _players.FirstOrDefault(player => player.TeamPlayerId == teamPlayerId);
			}
			catch (InvalidOperationException)
			{
				return null;
			}
		}

		public IEnumerator<Player> GetEnumerator()
		{
			return _players.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public void Add(Player item)
		{
			_players.Add(item);
		}

		public void Clear()
		{
			_players.Clear();
		}

		public bool Contains(Player item)
		{
			return (_players.Contains(item));
		}

		public void CopyTo(Player[] array, int arrayIndex)
		{
			_players.CopyTo(array, arrayIndex);
		}

		public bool Remove(Player item)
		{
			return _players.Remove(item);
		}

		public bool Remove(TeamPlayerId teamPlayerId)
		{
			return _players.Remove(Player(teamPlayerId));
		}

		public int Count
		{
			get { return _players.Count; }
		}

		public bool IsReadOnly
		{
			get { return false; }
		}
	}

	public struct TeamPlayerId
		: IEquatable<TeamPlayerId>
	{
		public TeamPlayerId(int playerNumber)
			: this()
		{
			PlayerNumber = playerNumber;
		}

		public TeamPlayerId(int teamNumber, int playerNumber)
			: this()
		{
			PlayerNumber = PlayerFromTeamAndTeamPlayer(teamNumber, playerNumber);
		}

		public const int MaximumPlayerNumber = 24;

		public int PlayerNumber { get; set; }

		public int TeamNumber
		{
			get { return PlayerNumber == 0 ? 0 : (((PlayerNumber - 1) & 0x1f) >> 3) + 1; }
			set { PlayerNumber = PlayerFromTeamAndTeamPlayer(value, TeamPlayerNumber); }
		}

		public int TeamPlayerNumber
		{
			get { return PlayerNumber == 0 ? 0 : ((PlayerNumber - 1) & 0x7) + 1; }
			set { PlayerNumber = PlayerFromTeamAndTeamPlayer(TeamNumber, value); }
		}

		public UInt16 Packed23
		{
			get { return (byte) (PlayerNumber == 0 ? 0 : ((TeamNumber & 0x3) << 3) | ((TeamPlayerNumber - 1) & 0x7)); }
			set
			{
				var teamNumber = (value >> 3) & 0x3;
				var teamPlayerNumber = (value & 0x7) + 1;
				PlayerNumber = PlayerFromTeamAndTeamPlayer(teamNumber, teamPlayerNumber);
			}
		}

		public UInt16 Packed34
		{
			get { return (UInt16) (PlayerNumber == 0 ? 0 : (Packed44 & 0x7f)); }
			set
			{
				var teamNumber = (value >> 4) & 0x7;
				var teamPlayerNumber = (value & 0xf) + 1;
				PlayerNumber = PlayerFromTeamAndTeamPlayer(teamNumber, teamPlayerNumber);
			}
		}

		public UInt16 Packed44
		{
			get { return (byte) (PlayerNumber == 0 ? 0 : (((TeamNumber & 0xf) << 4) | ((TeamPlayerNumber - 1) & 0xf))); }
			set
			{
				var teamNumber = (value >> 4) & 0xf;
				var teamPlayerNumber = (value & 0xf) + 1;
				PlayerNumber = PlayerFromTeamAndTeamPlayer(teamNumber, teamPlayerNumber);
			}
		}

		public static TeamPlayerId FromPacked23(UInt16 value)
		{
			return new TeamPlayerId {Packed23 = value};
		}

		public static TeamPlayerId FromPacked34(UInt16 value)
		{
			return new TeamPlayerId {Packed34 = value};
		}

		public static TeamPlayerId FromPacked44(UInt16 value)
		{
			return new TeamPlayerId {Packed44 = value};
		}

		private static int PlayerFromTeamAndTeamPlayer(int teamNumber, int teamPlayerNumber)
		{
			if (teamNumber == 0 || teamPlayerNumber == 0) return 0;
			return (((teamNumber & 0x3) - 1) << 3) + (((teamPlayerNumber - 1) & 0x7) + 1);
		}

		public static bool operator ==(TeamPlayerId first, TeamPlayerId second)
		{
			return first.PlayerNumber == second.PlayerNumber;
		}

		public static bool operator !=(TeamPlayerId first, TeamPlayerId second)
		{
			return !(first == second);
		}

		public bool Equals(TeamPlayerId other)
		{
			return this == other;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			return obj is TeamPlayerId && Equals((TeamPlayerId) obj);
		}

		public override int GetHashCode()
		{
			return PlayerNumber.GetHashCode();
		}

		public override string ToString()
		{
			return ToString(false);
		}

		public string ToString(bool teamGame)
		{
			if (teamGame)
			{
				return string.Format("T{0}:P{1}", TeamNumber, TeamPlayerNumber);
			}
			else
			{
				return PlayerNumber.ToString(CultureInfo.InvariantCulture);
			}
		}

		public string ToStringFull(bool teamGame)
		{
			if (teamGame)
			{
				return string.Format("Team {0}, Player {1}", TeamNumber, TeamPlayerNumber);
			}
			else
			{
				return string.Format("Player {0}", PlayerNumber);
			}
		}
	}
}
