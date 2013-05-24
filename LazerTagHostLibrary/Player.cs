using System;
using System.ComponentModel;
using System.Globalization;

namespace LazerTagHostLibrary
{
	public class Player
	{
		public byte GameSessionTaggerId { get; set; }
		public bool Confirmed = false;
		public bool Debriefed = false;
		public TeamPlayerId TeamPlayerId { get; set; }

		//damage taken during match
		public int TagsTaken = 0;
		//still alive at end of match
		public bool Survived { get; set; }
		//true if the debriefing stated a report was coming but one not received yet
		public bool[] ScoreReportTeamsWaiting = new[] {false, false, false};
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

		//final score for given game mode
		public int Score { get; set; }
		public int Rank { get; set; } //1-24
		public int TeamRank = 0; //1-3

		private string _playerName = "Unnamed Player";

		public string PlayerName
		{
			get { return _playerName; }
			set { _playerName = value; }
		}

		public Player(byte gameSessionTaggerId)
		{
			Survived = false;
			GameSessionTaggerId = gameSessionTaggerId;
		}

		public bool HasBeenDebriefed()
		{
			if (!Debriefed) return false;
			foreach (var scoreReportTeamAvailable in ScoreReportTeamsWaiting)
			{
				if (scoreReportTeamAvailable) return false;
			}
			return true;
		}
	}

	public struct TeamPlayerId
		: IEquatable<TeamPlayerId>
	{
		#region Constructors
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
		#endregion

		#region Public Constants
		public const int MaximumPlayerNumber = 24;
		#endregion

		#region Public Properties
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

		public UInt16 Packed44
		{
			get { return (byte) (((TeamNumber & 0xf) << 4) | ((TeamPlayerNumber - 1) & 0xf)); }
			set
			{
				TeamNumber = (value >> 4) & 0xf;
				TeamPlayerNumber = (value & 0xf) + 1;
			}
		}
		#endregion

		#region Public Methods
		public static TeamPlayerId FromPacked23(UInt16 value)
		{
			return new TeamPlayerId {Packed23 = value};
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
			return string.Format("{0} ({1}:{2})", PlayerNumber, TeamNumber, TeamPlayerNumber);
		}
		#endregion
	}
}
