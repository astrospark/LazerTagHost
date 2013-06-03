using System;
using System.Globalization;

namespace LazerTagHostLibrary
{
	public class Player
	{
		private HostGun _hostGun;
		public byte GameSessionTaggerId { get; set; }
		public bool Confirmed = false;
		public bool TagSummaryReceived = false;
		public TeamPlayerId TeamPlayerId { get; set; }

		public int TagsTaken = 0;
		public bool Survived { get; set; }
		public bool[] TeamTagReportsExpected = new[] {false, false, false};
		public bool[] TeamTagReportsReceived = new[] {false, false, false};

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

		public int Score { get; set; }
		public int Rank { get; set; } // 1-24
		public int TeamRank = 0; // 1-3

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

		public Player(HostGun hostGun, byte gameSessionTaggerId)
		{
			_hostGun = hostGun;
			GameSessionTaggerId = gameSessionTaggerId;
			Survived = false;
		}

		public bool AllTagReportsReceived()
		{
			if (!TagSummaryReceived) return false;

			for (var i = 0; i < 3; i++)
			{
				if (TeamTagReportsExpected[i] && !TeamTagReportsReceived[i]) return false;
			}

			return true;
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
			TeamNumber = teamNumber;
			TeamPlayerNumber = playerNumber;
		}

		public const int MaximumPlayerNumber = 24;
	
		public int PlayerNumber { get; set; }

		public int TeamNumber
		{
			get { return (((PlayerNumber - 1) & 0x1f) >> 3) + 1; }
			set { PlayerNumber = (((value & 0x3) - 1) << 3) + TeamPlayerNumber; }
		}

		public int TeamPlayerNumber
		{
			get { return ((PlayerNumber - 1) & 0x7) + 1; }
			set { PlayerNumber = ((TeamNumber - 1) * 8) + (value & 0xf); }
		}

		public UInt16 Packed23
		{
			get { return (byte) (((TeamNumber & 0x3) << 3) | ((TeamPlayerNumber - 1) & 0x7)); }
			set
			{
				TeamNumber = (value >> 3) & 0x3;
				TeamPlayerNumber = (value & 0x7) + 1;
			}
		}

		public UInt16 Packed34
		{
			get { return (UInt16) (Packed44 & 0x7f); }
			set
			{
				TeamNumber = (value >> 4) & 0x7;
				TeamPlayerNumber = (value & 0xf) + 1;
			}
		}

		public UInt16 Packed44
		{
			get { return (byte) (((TeamNumber & 0xf) << 4) | ((TeamPlayerNumber - 1) & 0xf)); }
			set
			{
				TeamNumber = (value >> 4) & 0xf;
				TeamPlayerNumber = (value & 0xf) + 1;
			}
		}

		public static TeamPlayerId FromPacked23(UInt16 value)
		{
			return new TeamPlayerId {Packed23 = value};
		}

		public static TeamPlayerId FromPacked34(UInt16 value)
		{
			return new TeamPlayerId { Packed34 = value };
		}

		public static TeamPlayerId FromPacked44(UInt16 value)
		{
			return new TeamPlayerId {Packed44 = value};
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
			return obj is TeamPlayerId && Equals((TeamPlayerId)obj);
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
				return string.Format("{0}:{1}", TeamNumber, TeamPlayerNumber);
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
