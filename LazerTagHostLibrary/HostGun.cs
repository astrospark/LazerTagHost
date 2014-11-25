using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace LazerTagHostLibrary
{
    public class HostGun
    {
	    private class JoinState
	    {
		    public UInt16 GameId;
		    public Player Player;
		    public DateTime AssignPlayerSendTime;
		    public bool Failed;
		    public int AssignPlayerFailSendCount;
		    public DateTime LastAssignPlayerFailSendTime;
	    };

        public enum HostingStates
		{
            Idle,
            Adding,
			AcknowledgePlayerAssignment,
            Countdown,
			ResendCountdown,
			Playing,
            Summary,
            GameOver,
        };

		public enum ZoneType
		{
			// Invalid = 0x0,		// 00
			// Reserved = 0x1,		// 01
			ContestedZone = 0x2,	// 10
			TeamZone = 0x3			// 11
		}

		private readonly TeamCollection _teams = new TeamCollection();
		public TeamCollection Teams
	    {
			get { return _teams; }
	    }

		private readonly PlayerCollection _players = new PlayerCollection();
		public PlayerCollection Players
	    {
		    get { return _players; }
	    }

	    public int TeamCount
	    {
			get { return _gameDefinition.TeamCount; }
	    }

		public LazerTagProtocol Protocol { get; set; }

        private const int GameAnnouncementFrequencyMilliseconds = 1500;
		private const int AcknowledgePlayerAssignmentTimeoutSeconds = 2;
		private const int AssignPlayerFailedSendCount = 6;
		private const int AssignPlayerFailedFrequencyMilliseconds = 500;
        private const int MinimumPlayerCount = 2;
        private const int RequestTagReportFrequencySeconds = 3;
        private const int GameOverAnnouncementFrequencySeconds = 3;
	    private const int ZoneBeaconFrequencyMilliseconds = 500;
        
        private GameDefinition _gameDefinition;
	    public GameDefinition GameDefinition
	    {
		    get { return _gameDefinition; }
	    }

	    public HostingStates HostingState;

		public bool AllowLateJoins { get; set; } // TODO: Implement

        private readonly Dictionary<UInt16, JoinState> _joinStates = new Dictionary<ushort, JoinState>();

        private DateTime _stateChangeTimeout;
        private DateTime _nextAnnouncement;
	    private int _debriefPlayerSequence;
		private int _rankReportTeamNumber;

	    private DateTime _resendCountdownPlayingStateChangeTimeout;

		public HostGun(string portName)
		{
			HostingState = HostingStates.Idle;
			Protocol = new LazerTagProtocol(this, portName);
		}

		public void SetGameStartCountdownTime(int countdownTimeSeconds)
		{
            _gameDefinition.CountdownTimeSeconds = countdownTimeSeconds;
        }

	    private bool AssignTeamAndPlayer(int requestedTeam, Player newPlayer)
	    {
		    int assignedTeamNumber;
		    var assignedPlayerNumber = 0;

		    if (_players.Count >= TeamPlayerId.MaximumPlayerNumber)
		    {
			    Log.Add(Log.Severity.Information, "Cannot add player. The game is full.");
			    return false;
		    }

		    if (GameDefinition.IsTeamGame)
		    {
			    var isTagMasterGame = (
					GameDefinition.GameType == GameType.TagMasterHideAndSeek ||
					GameDefinition.GameType == GameType.TagMasterHuntThePrey ||
					GameDefinition.GameType == GameType.HuntTheTagMasterTwoTeams);

			    // Count the players on each team and find the smallest team
			    var teamPlayerCounts = new int[_gameDefinition.TeamCount];
			    var smallestTeamNumber = 0;
			    var smallestTeamPlayerCount = 8;
			    for (var teamNumber = 1; teamNumber <= _gameDefinition.TeamCount; teamNumber++)
			    {
				    var teamPlayerCount = 0;
				    foreach (var player in _players)
				    {
					    if (player.TeamPlayerId.TeamNumber == teamNumber) teamPlayerCount++;
				    }

					// In Tag Master games, team 1 is full once it has a player
					if (isTagMasterGame && teamNumber == 1 && teamPlayerCount > 0) teamPlayerCount = 8;

				    if (teamPlayerCount < smallestTeamPlayerCount)
				    {
					    smallestTeamNumber = teamNumber;
					    smallestTeamPlayerCount = teamPlayerCount;
				    }
				    teamPlayerCounts[teamNumber - 1] = teamPlayerCount;
			    }

			    if (smallestTeamNumber == 0)
			    {
					Log.Add(Log.Severity.Information, "All teams are full.");
					return false;
			    }

			    if (requestedTeam > 0 &&
			        requestedTeam <= _gameDefinition.TeamCount &&
			        teamPlayerCounts[requestedTeam - 1] < 8)
			    {
				    assignedTeamNumber = requestedTeam;
			    }
			    else
			    {
				    assignedTeamNumber = smallestTeamNumber;
			    }

			    for (var playerNumber = 1; playerNumber <= 8; playerNumber++)
			    {
				    if (_players.Player(new TeamPlayerId(assignedTeamNumber, playerNumber)) == null)
				    {
					    assignedPlayerNumber = playerNumber;
					    break;
				    }
			    }
		    }
		    else
		    {
			    assignedTeamNumber = 1;
			    // Assign player to the first open player number
			    for (var playerNumber = 1; playerNumber <= 24; playerNumber++)
			    {
				    if (_players.Player(new TeamPlayerId(playerNumber)) != null) continue;
				    assignedPlayerNumber = playerNumber;
					break;
			    }
		    }

		    if (assignedPlayerNumber == 0)
		    {
			    Log.Add(Log.Severity.Warning, "Unable to assign a player number.");
			    return false;
		    }

		    if (_gameDefinition.IsTeamGame)
		    {
				newPlayer.TeamPlayerId = new TeamPlayerId(assignedTeamNumber, assignedPlayerNumber);
		    }
		    else
		    {
				newPlayer.TeamPlayerId = new TeamPlayerId(assignedPlayerNumber);
			}

			newPlayer.Team = Teams.Team(assignedTeamNumber);
			Teams.Team(assignedTeamNumber).Players.Add(newPlayer);

		    return true;
	    }

	    private static void AssertUnknownBits(String name, Signature signature, byte mask)
        {
	        if (((byte) (signature.Data & mask)) == 0) return;
		    Log.Add(Log.Severity.Warning, "Unknown bits set: \"{0}\", data: 0x{2:X2}, mask: 0x{3:X2}, unknown: 0x{1:X2}",
			    name, (byte) (signature.Data & mask), (byte) signature.Data, mask);
        }
        
        private void PrintScoreReport()
        {
            foreach (var player in _players)
            {
	            Log.Add(Log.Severity.Information, "{0} ({1})", player.NameAndNumber, player.TaggerId);
				if (_gameDefinition.IsTeamGame)
				{
					Log.Add(Log.Severity.Information, "\tPlayer Rank: {0}, Team Rank: {1}, Score: {2}", player.Rank, player.Team.Rank, player.Score);
					for (var teamNumber = 1; teamNumber <= 3; teamNumber++)
					{
						var taggedByPlayerCounts = new int[8];
						for (var playerNumber = 1; playerNumber <= 8; playerNumber++)
						{
							var teamPlayerId = new TeamPlayerId(teamNumber, playerNumber);
							taggedByPlayerCounts[playerNumber - 1] = player.TaggedByPlayerCounts[teamPlayerId.PlayerNumber - 1];
						}
						Log.Add(Log.Severity.Information, "\tTags taken from team {0}: {1}", teamNumber, String.Join(", ", taggedByPlayerCounts));
					}
				}
				else
				{
					Log.Add(Log.Severity.Information, "\tPlayer Rank: {0}, Score: {1}", player.Rank, player.Score);
					var taggedByPlayerCounts = new int[24];
					for (var playerNumber = 1; playerNumber <= 24; playerNumber++)
					{
						var teamPlayerId = new TeamPlayerId(playerNumber);
						taggedByPlayerCounts[playerNumber - 1] = player.TaggedByPlayerCounts[teamPlayerId.PlayerNumber - 1];
					}
					Log.Add(Log.Severity.Information, "\tTags taken from players: {0}", String.Join(", ", taggedByPlayerCounts));
				}
            }
        }

	    public bool ProcessPacket(Packet packet)
	    {
		    var packetType = (PacketType) packet.PacketTypeSignature.Data;
		    switch (packetType)
		    {
			    case PacketType.SingleTagReport:
				    if (packet.Data.Count != 4) break;
				    var isReply = ((packet.Data[1].Data & 0x80) & (packet.Data[2].Data & 0x80)) != 0;
				    var teamPlayerId1 = TeamPlayerId.FromPacked34(packet.Data[1].Data);
				    var teamPlayerId2 = TeamPlayerId.FromPacked34(packet.Data[2].Data);
				    var tagsReceived = packet.Data[3].Data;
				    var replyText = isReply ? "replied to tag count request from" : "requested tag count from";
				    Log.Add(Log.Severity.Information, "Player {0} {1} player {2}. Player {0} received {3} tags from player {2}.",
					    teamPlayerId1, replyText, teamPlayerId2, tagsReceived);
				    return true;
			    case PacketType.TextMessage:
				    var message = new StringBuilder();
				    var i = 0;
				    while (i < packet.Data.Count &&
							packet.Data[i].Data >= 0x20 &&
							packet.Data[i].Data <= 0x7e &&
							packet.Data[i].BitCount == 8)
				    {
					    message.Append(Convert.ToChar(packet.Data[i].Data));
					    i++;
				    }
				    Log.Add(Log.Severity.Information, "Received Text Message: {0}", message);
				    return true;
			    case PacketType.SpecialAttack:
				    var type = "Unknown Type";
				    if (packet.Data.Count == 4)
				    {
					    switch (packet.Data[2].Data)
					    {
						    case 0x77:
							    type = "EM Peacemaker";
							    break;
						    case 0xb1:
							    type = "Talus Airstrike";
							    break;
					    }
				    }
				    Log.Add(Log.Severity.Information, "Special Attack: {0} - {1}", type, packet);
				    AssertUnknownBits("Special Attack Flags 1", packet.Data[1], 0xff);
				    AssertUnknownBits("Special Attack Flags 2", packet.Data[2], 0xff);
				    AssertUnknownBits("Special Attack Flags 3", packet.Data[3], 0xff);
				    AssertUnknownBits("Special Attack Flags 4", packet.Data[4], 0xff);
				    return true;
		    }

		    switch (HostingState)
		    {
			    case HostingStates.Idle:
				    return true;
			    case HostingStates.Adding:
			    case HostingStates.AcknowledgePlayerAssignment:
				    if (packet.Data.Count < 2) return false;

				    var gameId = packet.Data[0].Data;
				    var taggerId = packet.Data[1].Data;

				    switch (packet.Type)
				    {
					    case PacketType.RequestJoinGame:
						    if (packet.Data.Count < 3) return false;
						    var requestedTeam = (UInt16) (packet.Data[2].Data & 0x03);
						    return ProcessRequestJoinGame(gameId, taggerId, requestedTeam);
					    case PacketType.AcknowledgePlayerAssignment:
						    return ProcessAcknowledgePlayerAssignment(gameId, taggerId);
					    default:
						    Log.Add(Log.Severity.Warning, "Unexpected packet: {0}", packet);
						    return false;
				    }
			    case HostingStates.Summary:
				    switch (packet.Type)
				    {
					    case PacketType.TagSummary:
						    return ProcessTagSummary(packet);
					    case PacketType.TeamOneTagReport:
					    case PacketType.TeamTwoTagReport:
					    case PacketType.TeamThreeTagReport:
						    return ProcessTeamTagReport(packet);
				    }
				    break;
		    }

		    return false;
	    }

	    private bool ProcessRequestJoinGame(UInt16 gameId, UInt16 taggerId, UInt16 requestedTeam)
	    {
			// TODO: Handle multiple simultaneous games
			if (gameId != GameDefinition.GameId)
			{
				Log.Add(Log.Severity.Warning, "Wrong game ID.");
				return false;
			}

			Player player = null;

			foreach (var checkPlayer in Players)
			{
				if (checkPlayer.TaggerId == taggerId && !checkPlayer.Confirmed)
				{
					player = checkPlayer;
					break;
				}
			}

			if (player == null)
			{
				player = new Player(this, (byte)taggerId);

				if (!AssignTeamAndPlayer(requestedTeam, player)) return false;

				Players.Add(player);
			}

			var joinState = new JoinState
			{
				GameId = gameId,
				Player = player,
				AssignPlayerSendTime = DateTime.Now
			};
			_joinStates.Remove(taggerId);
			_joinStates.Add(taggerId, joinState);

			Log.Add(Log.Severity.Information, "Assigning tagger 0x{0:X2} to player {1} for game 0x{2:X2}.", taggerId,
				   player.TeamPlayerId.ToString(_gameDefinition.IsTeamGame), gameId);

		    SendPlayerAssignment(player.TeamPlayerId);

			ChangeState(HostingStates.AcknowledgePlayerAssignment);

			return true;
		}

		private bool ProcessAcknowledgePlayerAssignment(UInt16 gameId, UInt16 taggerId)
		{
			// TODO: Handle multiple simultaneous games
			if (gameId != _gameDefinition.GameId)
			{
				Log.Add(Log.Severity.Warning, "Wrong game ID.");
				return false;
			}

			JoinState joinState;
			if (!_joinStates.TryGetValue(taggerId, out joinState))
			{
				Log.Add(Log.Severity.Warning, "Unable to find player to confirm");
				return false;
			}

			var player = joinState.Player;
			player.Confirmed = true;
			_joinStates.Remove(taggerId);

			Log.Add(Log.Severity.Information, "Confirmed player {0} for game 0x{1:X2}.",
			                   player.TeamPlayerId.ToString(_gameDefinition.IsTeamGame), gameId);

			if (_joinStates.Count < 1) ChangeState(HostingStates.Adding);

			OnPlayerListChanged(new PlayerListChangedEventArgs(Players));

			return true;
		}

		private bool ProcessTagSummary(Packet packet)
		{
			if (packet.Type != PacketType.TagSummary)
			{
				Log.Add(Log.Severity.Warning, "Unexpected packet: {0}", packet);
				return false;
			}

			if (packet.Data.Count != 8)
			{
				return false;
			}

			var gameId = packet.Data[0].Data;
			if (gameId != _gameDefinition.GameId)
			{
				Log.Add(Log.Severity.Warning, "Wrong game ID.");
				return false;
			}

			var teamPlayerId = TeamPlayerId.FromPacked44(packet.Data[1].Data);
			var tagsTaken = packet.Data[2].Data; // Hex Coded Decimal
			var surviveTimeMinutes = packet.Data[3].Data;
			var surviveTimeSeconds = packet.Data[4].Data;
			var zoneTimeMinutes = packet.Data[5].Data;
			var zoneTimeSeconds = packet.Data[6].Data;

			var teamTagReports = packet.Data[7];
			AssertUnknownBits("teamTagReports", teamTagReports, 0xf1);

			var player = _players.Player(teamPlayerId);
			if (player == null)
			{
				Log.Add(Log.Severity.Warning, "Unable to find player for score report.");
				return false;
			}

			player.SurviveTime = new TimeSpan(0, 0, BinaryCodedDecimal.ToDecimal(surviveTimeMinutes), BinaryCodedDecimal.ToDecimal(surviveTimeSeconds));
			player.Survived = player.SurviveTime.TotalMinutes >= GameDefinition.GameTimeMinutes;
			player.TagsTaken = BinaryCodedDecimal.ToDecimal(tagsTaken);
			player.TeamTagReportsExpected[0] = (teamTagReports.Data & 0x2) != 0;
			player.TeamTagReportsExpected[1] = (teamTagReports.Data & 0x4) != 0;
			player.TeamTagReportsExpected[2] = (teamTagReports.Data & 0x8) != 0;
			player.ZoneTime = new TimeSpan(0, 0, BinaryCodedDecimal.ToDecimal(zoneTimeMinutes), BinaryCodedDecimal.ToDecimal(zoneTimeSeconds));

			player.TagSummaryReceived = true;

			Log.Add(Log.Severity.Information, "Received tag summary from {0}.", player.NameAndNumber);

			OnPlayerListChanged(new PlayerListChangedEventArgs(Players));

			return true;
		}

		private bool ProcessTeamTagReport(Packet packet)
		{
			if (packet.Data.Count < 4) return false;

			var gameId = packet.Data[0].Data;
			if (gameId != _gameDefinition.GameId)
			{
				Log.Add(Log.Severity.Warning, "Wrong game ID.");
				return false;
			}

			// what team do the scores relate to hits from
			var taggedByTeamNumber = (int)(packet.PacketTypeSignature.Data - PacketType.TeamOneTagReport + 1);

			var teamPlayerId = TeamPlayerId.FromPacked44(packet.Data[1].Data);

			var player = Players.Player(teamPlayerId);
			if (player == null) return false;

			if (player.TagSummaryReceived && !player.TeamTagReportsExpected[taggedByTeamNumber - 1])
			{
				Log.Add(Log.Severity.Warning, "A tag report from player {0} for team {1} was not expected.",
					player.TeamPlayerId, taggedByTeamNumber);
			}

			if (player.TeamTagReportsReceived[taggedByTeamNumber - 1])
			{
				Log.Add(Log.Severity.Warning, "A tag report from player {0} for team {1} was already received. Discarding.",
					player.TeamPlayerId, taggedByTeamNumber);
				return false;
			}

			player.TeamTagReportsReceived[taggedByTeamNumber - 1] = true;

			var scoreBitmask = packet.Data[2].Data;

			var packetIndex = 3;
			var mask = scoreBitmask;
			for (var taggedByTeamPlayerNumber = 1; taggedByTeamPlayerNumber <= 8; taggedByTeamPlayerNumber++)
			{
				var taggedByTeamPlayerId = new TeamPlayerId(taggedByTeamNumber, taggedByTeamPlayerNumber);
				var hasTags = ((mask >> (taggedByTeamPlayerNumber - 1)) & 0x1) != 0;
				if (!hasTags) continue;

				if (packet.Data.Count <= packetIndex)
				{
					Log.Add(Log.Severity.Warning, "Ran off end of score report");
					return false;
				}

				var tagsTaken = BinaryCodedDecimal.ToDecimal(packet.Data[packetIndex].Data);

				player.TaggedByPlayerCounts[taggedByTeamPlayerId.PlayerNumber - 1] = tagsTaken;

				var taggedByPlayer = Players.Player(taggedByTeamPlayerId);
				if (taggedByPlayer == null) continue;
				taggedByPlayer.TaggedPlayerCounts[player.TeamPlayerId.PlayerNumber - 1] = tagsTaken;

				packetIndex++;
			}

			OnPlayerListChanged(new PlayerListChangedEventArgs(Players));

			return true;
		}

        private void CalculateScores()
        {
	        RemoveDroppedPlayerTags();

            switch (_gameDefinition.GameType)
			{
				case GameType.OwnTheZone:
				case GameType.OwnTheZoneTwoTeams:
				case GameType.OwnTheZoneThreeTeams:
					CalculateScoresOwnTheZone();
					break;
				case GameType.CustomLazerTag:
				case GameType.Respawn:
					CalculateScoresSoloGames();
					break;
				case GameType.CustomLazerTagTwoTeams:
				case GameType.CustomLazerTagThreeTeams:
				case GameType.HuntThePrey:
				case GameType.HideAndSeek:
				case GameType.RespawnTwoTeams:
				case GameType.RespawnThreeTeams:
				case GameType.TagMasterHideAndSeek:
				case GameType.TagMasterHuntThePrey:
					CalculateScoresTeamGames();
					break;
				case GameType.KingsTwoTeams:
				case GameType.KingsThreeTeams:
					CalculateScoresKings();
					break;
				case GameType.OneOnOne:
					CalculateScoresOneOnOne();
					break;
				case GameType.Survival:
					CalculateScoresSurvivalSolo();
					break;
				case GameType.SurvivalTwoTeams:
				case GameType.SurvivalThreeTeams:
					CalculateScoresSurvivalTeams();
					break;
				case GameType.HuntTheTagMasterTwoTeams:
					CalculateScoresHuntTheTagMasterTwoTeams();
					break;
				default:
					Log.Add(Log.Severity.Warning, "Unable to score game type {0}.", _gameDefinition.GameType);
					break;
			}
        }

		private void RemoveDroppedPlayerTags()
	    {
		    foreach (var droppedPlayer in Players.Where(droppedPlayer => droppedPlayer.Dropped))
		    {
			    droppedPlayer.TagsTaken = 0;

			    foreach (var otherPlayer in Players)
			    {
				    droppedPlayer.TaggedPlayerCounts[otherPlayer.TeamPlayerId.PlayerNumber - 1] = 0;
				    droppedPlayer.TaggedByPlayerCounts[otherPlayer.TeamPlayerId.PlayerNumber - 1] = 0;

				    otherPlayer.TagsTaken -= otherPlayer.TaggedByPlayerCounts[droppedPlayer.TeamPlayerId.PlayerNumber - 1];
				    otherPlayer.TaggedPlayerCounts[droppedPlayer.TeamPlayerId.PlayerNumber - 1] = 0;
				    otherPlayer.TaggedByPlayerCounts[droppedPlayer.TeamPlayerId.PlayerNumber - 1] = 0;
			    }
		    }
	    }

	    private void CalculateScoresOwnTheZone()
		{
			foreach (var player in _players)
			{
				var playerZoneTimeSeconds = Convert.ToInt32(player.ZoneTime.TotalSeconds);
				player.Score = playerZoneTimeSeconds;
				Teams.Team(player.TeamPlayerId.TeamNumber).Score += playerZoneTimeSeconds;
			}

			Teams.CalculateRanks();
			Players.CalculateRanks();
		}

		private void CalculateScoresSoloGames()
		{
			foreach (var player in _players)
			{
				// Players lose 1 point for each tag they receive from another player
				player.Score = -player.TagsTaken;

				for (var playerNumber = 1; playerNumber <= TeamPlayerId.MaximumPlayerNumber; playerNumber++)
				{
					// Players receive 2 points for each tag they land on another player
					player.Score += 2*player.TaggedPlayerCounts[playerNumber - 1];
				}
			}

		    Players.CalculateRanks();
		}

		private void CalculateScoresTeamGames()
	    {
			var teamCount = GameDefinition.TeamCount;
			var teamSurvivedPlayerCounts = new int[teamCount];
			var teamSurvivedPlayerScoreTotals = new int[teamCount];

			// Calculate player scores
		    foreach (var player in _players)
		    {
				// Players lose 1 point for each tag they receive from another player
				var score = -player.TagsTaken;

				for (var teamNumber = 1; teamNumber <= teamCount; teamNumber++)
			    {
				    for (var playerNumber = 1; playerNumber <= 8; playerNumber++)
				    {
					    var teamPlayerId = new TeamPlayerId(teamNumber, playerNumber);
					    if (player.TeamPlayerId.TeamNumber == teamNumber)
					    {
							// Players lose 2 points for each tag they land on players on their own team
							score -= 2 * player.TaggedPlayerCounts[teamPlayerId.PlayerNumber - 1];
					    }
					    else
					    {
							// Players receive 2 points for each tag they land on players from other teams
							score += 2 * player.TaggedPlayerCounts[teamPlayerId.PlayerNumber - 1];
					    }
				    }
			    }

			    player.Score = score;
			    if (player.Survived)
			    {
				    teamSurvivedPlayerCounts[player.TeamPlayerId.TeamNumber - 1]++;
				    teamSurvivedPlayerScoreTotals[player.TeamPlayerId.TeamNumber - 1] += score;
			    }
		    }

		    // Calculate team scores
			for (var teamNumber = 1; teamNumber <= teamCount; teamNumber++)
			{
				var teamScore = (teamSurvivedPlayerCounts[teamNumber - 1] << 10) + (teamSurvivedPlayerScoreTotals[teamNumber - 1] << 2);
				Teams.Team(teamNumber).Score = teamScore;
				Log.Add(Log.Severity.Information, "Team {0} had {1} surviving players.", teamNumber, teamSurvivedPlayerCounts[teamNumber - 1]);
				Log.Add(Log.Severity.Information, "The total score of the surviving players was {0}.", teamSurvivedPlayerScoreTotals[teamNumber - 1]);
				Log.Add(Log.Severity.Information, "Team {0}'s final score was {1}.", teamNumber, teamScore);
		    }

		    Teams.CalculateRanks();
		    Players.CalculateRanks();
	    }

		private void CalculateScoresKings()
		{
			// TODO: Check that this scoring matches that calculated by the taggers

			var teamCount = GameDefinition.TeamCount;
			var teamKingSurvived = new bool[teamCount];
			var teamKingTagsTaken = new int[teamCount];

			// Calculate player scores
			foreach (var player in _players)
			{
				if (player.TeamPlayerId.TeamPlayerNumber == 1) // player is a king
				{
					var teamNumber = player.TeamPlayerId.TeamNumber - 1;
					teamKingSurvived[teamNumber] = player.Survived;
					teamKingTagsTaken[teamNumber] = player.TagsTaken;
					Log.Add(Log.Severity.Information, "Team {0}'s king took {1} tags and {2}.", teamNumber, player.TagsTaken, player.Survived ? "survived" : "did not survive");
				}

				for (var teamNumber = 1; teamNumber <= teamCount; teamNumber++)
				{
					var teamKing = Players.Player(new TeamPlayerId(teamNumber, 1));
					if (teamKing == null)
					{
						Log.Add(Log.Severity.Warning, "Could not find the king for team {0}.", teamNumber);
						return;
					}

					if (player.TeamPlayerId.TeamNumber == teamNumber)
					{
						// Players lose 4 point for each tag they land on their own team's king
						player.Score -= 4*player.TaggedPlayerCounts[teamKing.TeamPlayerId.PlayerNumber - 1];
					}
					else
					{
						// Players receive 1 point for each tag they land on other teams' kings
						player.Score -= player.TaggedPlayerCounts[teamKing.TeamPlayerId.PlayerNumber - 1];
					}
				}
			}

			// Calculate team scores
			for (var teamNumber = 1; teamNumber <= teamCount; teamNumber++)
			{
				var teamScore = (teamKingSurvived[teamNumber - 1] ? 1 : 0 << 10) - (teamKingTagsTaken[teamNumber - 1] << 2);
				Teams.Team(teamNumber).Score = teamScore;
			}

			Teams.CalculateRanks();
			Players.CalculateRanks();
		}

		private void CalculateScoresOneOnOne()
		{
			var teamCount = GameDefinition.TeamCount;
			var teamSurvivedPlayerCounts = new int[teamCount];
			var teamScoreTotals = new int[teamCount];

			// Calculate player scores
			foreach (var player in _players)
			{
				// Players lose 1 point for each tag they receive from another player
				var score = -player.TagsTaken;

				for (var teamNumber = 1; teamNumber <= teamCount; teamNumber++)
				{
					for (var playerNumber = 1; playerNumber <= 8; playerNumber++)
					{
						var teamPlayerId = new TeamPlayerId(teamNumber, playerNumber);
						if (player.TeamPlayerId.TeamNumber == teamNumber)
						{
							// Players lose 2 points for each tag they land on players on their own team
							score -= 2*player.TaggedPlayerCounts[teamPlayerId.PlayerNumber - 1];
						}
						else
						{
							// Players receive 2 points for each tag they land on their principal opponent
							// Players receive 1 point for each tag they land on other players from other teams
							var isPrincipalOpponent = playerNumber == player.TeamPlayerId.TeamPlayerNumber;
							var multiplier = isPrincipalOpponent ? 2 : 1;
							score += multiplier*player.TaggedPlayerCounts[teamPlayerId.PlayerNumber - 1];
						}
					}
				}

				player.Score = score;
				teamScoreTotals[player.TeamPlayerId.TeamNumber - 1] += score;

				if (player.Survived) teamSurvivedPlayerCounts[player.TeamPlayerId.TeamNumber - 1]++;
			}

			// Calculate team scores
			for (var teamNumber = 1; teamNumber <= teamCount; teamNumber++)
			{
				var teamScore = teamScoreTotals[teamNumber - 1];
				Teams.Team(teamNumber).Score = teamScore;
				// TODO: Break ties based on survived player count
				Log.Add(Log.Severity.Information, "Team {0} had {1} surviving players.", teamNumber, teamSurvivedPlayerCounts[teamNumber - 1]);
				Log.Add(Log.Severity.Information, "The total score of the team was {0}.", teamScore);
		    }

		    Teams.CalculateRanks();
		    Players.CalculateRanks();
		}

		private void CalculateScoresSurvivalSolo()
		{
			foreach (var player in _players)
			{
				// Players lose 1 point for each tag they receive from another player
				player.Score = -player.TagsTaken;

				for (var playerNumber = 1; playerNumber <= TeamPlayerId.MaximumPlayerNumber; playerNumber++)
				{
					// Players receive 10 points for each full minute they survived
					player.Score += 10*(int) player.SurviveTime.TotalMinutes;

					// Players receive 2 points for each tag they land on another player
					player.Score += 2*player.TaggedPlayerCounts[playerNumber - 1];
				}
			}

			Players.CalculateRanks();
		}

		private void CalculateScoresSurvivalTeams()
		{
			// TODO: Calculate
			throw new NotImplementedException();
		}

		private void CalculateScoresHuntTheTagMasterTwoTeams()
		{
			var teamCount = GameDefinition.TeamCount;
			var teamSurvivedPlayerCounts = new int[teamCount];
			var teamScoreTotals = new int[teamCount];

			// Calculate player scores
			foreach (var player in _players)
			{
				var score = 0;

				for (var teamNumber = 1; teamNumber <= teamCount; teamNumber++)
				{
					for (var teamPlayerNumber = 1; teamPlayerNumber <= 8; teamPlayerNumber++)
					{
						var playerNumber = new TeamPlayerId(teamNumber, teamPlayerNumber).PlayerNumber;

						if (player.TeamPlayerId.TeamNumber == 1) // Tag Master
						{
							if (teamNumber != 1) // vs other players
							{
								// The Tag Master loses 1 point for each tag they received from other players
								score -= player.TaggedByPlayerCounts[playerNumber - 1];

								// The Tag Master receives 2 points for each tag they land on other players
								score += 2*player.TaggedPlayerCounts[playerNumber - 1];
							}
						}
						else // other players
						{
							if (teamNumber == 1) // vs Tag Master
							{
								// Players lose 1 point for each tag they received from the Tag Master
								score -= player.TaggedByPlayerCounts[playerNumber - 1];

								// Players receive 2 points for each tag they land on the Tag Master
								score += 2*player.TaggedPlayerCounts[playerNumber - 1];
							}
							else if (teamNumber == player.TeamPlayerId.TeamNumber) // vs same team
							{
								// Players lose 2 points for each tag they land on players on their own team
								score -= 2*player.TaggedPlayerCounts[playerNumber - 1];
							}
							// Score is not affected by players tagging or being tagged by the other team
						}
					}
				}

				player.Score = score;
				teamScoreTotals[player.TeamPlayerId.TeamNumber - 1] += score;

				if (player.Survived) teamSurvivedPlayerCounts[player.TeamPlayerId.TeamNumber - 1]++;
			}

			// Calculate team scores
			for (var teamNumber = 1; teamNumber <= teamCount; teamNumber++)
			{
				var teamScore = teamScoreTotals[teamNumber - 1];
				Teams.Team(teamNumber).Score = teamScore;
				Log.Add(Log.Severity.Information, "Team {0} had {1} surviving players.", teamNumber, teamSurvivedPlayerCounts[teamNumber - 1]);
				Log.Add(Log.Severity.Information, "The total score of the team was {0}.", teamScore);
			}

			Teams.CalculateRanks();
			Players.CalculateRanks();

			// Adjust the ranks based on whether or not the Tag Master survived
			if (Players.Player(new TeamPlayerId(1, 1)).Survived) // The Tag Master survived
			{
				Teams.Team(1).Rank = 1;

				if (Teams.Team(2).Rank < Teams.Team(3).Rank)
				{
					Teams.Team(2).Rank = 2;
					Teams.Team(3).Rank = 3;
				}
				else if (Teams.Team(2).Rank == Teams.Team(3).Rank)
				{
					Teams.Team(2).Rank = 2;
					Teams.Team(3).Rank = 2;
				}
				else
				{
					Teams.Team(2).Rank = 3;
					Teams.Team(3).Rank = 2;
				}
			}
			else // the Tag Master did not survive
			{
				Teams.Team(1).Rank = 3;

				if (Teams.Team(2).Rank < Teams.Team(3).Rank)
				{
					Teams.Team(2).Rank = 1;
					Teams.Team(3).Rank = 2;
				}
				else if (Teams.Team(2).Rank == Teams.Team(3).Rank)
				{
					Teams.Team(2).Rank = 1;
					Teams.Team(3).Rank = 1;
				}
				else
				{
					Teams.Team(2).Rank = 2;
					Teams.Team(3).Rank = 1;
				}
			}
		}

        private bool ChangeState(HostingStates state)
        {
	        var previousState = HostingState;

	        switch (state)
	        {
		        case HostingStates.Idle:
			        _players.Clear();
			        break;
		        case HostingStates.Countdown:
					if (previousState != HostingStates.Adding) return false;
			        Log.Add(Log.Severity.Information, "Starting countdown");
			        _stateChangeTimeout = DateTime.Now.AddSeconds(_gameDefinition.CountdownTimeSeconds);
			        break;
		        case HostingStates.ResendCountdown:
					if (previousState != HostingStates.Playing) return false;
					Log.Add(Log.Severity.Information, "Resending countdown");
			        _resendCountdownPlayingStateChangeTimeout = _stateChangeTimeout;
			        _stateChangeTimeout = DateTime.Now.AddSeconds(_gameDefinition.ResendCountdownTimeSeconds);
			        break;
		        case HostingStates.Adding:
					Log.Add(Log.Severity.Information, "Adding players");

					if (previousState != HostingStates.AcknowledgePlayerAssignment)
			        {
				        Teams.Clear();
				        if (_gameDefinition.IsTeamGame)
				        {
					        for (var teamNumber = 1; teamNumber <= _gameDefinition.TeamCount; teamNumber++)
					        {
						        Teams.Add(new Team(teamNumber));
					        }
				        }
				        else
				        {
					        Teams.Add(new Team(1));
				        }

				        _joinStates.Clear();
			        }
			        break;
		        case HostingStates.AcknowledgePlayerAssignment:
			        Log.Add(Log.Severity.Debug, "Waiting for AcknowledgePlayerAssignment packet.");
			        _stateChangeTimeout = DateTime.Now.AddSeconds(AcknowledgePlayerAssignmentTimeoutSeconds);
			        break;
		        case HostingStates.Playing:
					switch (previousState)
			        {
				        case HostingStates.Countdown:
					        Log.Add(Log.Severity.Information, "Starting Game");
					        _stateChangeTimeout = DateTime.Now.AddMinutes(_gameDefinition.GameTimeMinutes);
					        break;
				        case HostingStates.ResendCountdown:
					        Log.Add(Log.Severity.Information, "Continuing Game");
					        _stateChangeTimeout = _resendCountdownPlayingStateChangeTimeout;
					        break;
			        }
			        break;
		        case HostingStates.Summary:
			        Log.Add(Log.Severity.Information, "Debriefing");
			        break;
		        case HostingStates.GameOver:
			        Log.Add(Log.Severity.Information, "Debriefing completed");
			        _rankReportTeamNumber = 1;
			        break;
		        default:
			        return false;
	        }

	        HostingState = state;
			_nextAnnouncement = DateTime.Now;

	        OnHostingStateChanged(new HostingStateChangedEventArgs(previousState, state));
		
            return true;
        }

#region PublicInterface
        public void StartServer(GameDefinition gameDefinition)
		{
            if (HostingState != HostingStates.Idle) return;

			_gameDefinition = gameDefinition;
			_gameDefinition.GameId = new RandomId();

            ChangeState(HostingStates.Adding);
        }

        public void EndGame()
		{
            ChangeState(HostingStates.Idle);
	        _stateChangeTimeout = new DateTime(0);
		}

        public void DelayGame(int seconds)
		{
			_stateChangeTimeout = _stateChangeTimeout.AddSeconds(seconds);
        }

	    public void Next()
		{
            switch (HostingState)
			{
				case HostingStates.Adding:
					ChangeState(HostingStates.Countdown);
					break;
				case HostingStates.Playing:
					ChangeState(HostingStates.Summary);
					break;
				case HostingStates.Summary:
					foreach (var player in Players)
					{
						if (!player.AllTagReportsReceived()) DropPlayer(player.TeamPlayerId);
					}
					ChangeState(HostingStates.GameOver);
					break;
				case HostingStates.Idle:
				case HostingStates.AcknowledgePlayerAssignment:
				case HostingStates.Countdown:
				case HostingStates.ResendCountdown:
				case HostingStates.GameOver:
				default:
					Log.Add(Log.Severity.Warning, "Next cannot be used while in the {0} hosting state.", HostingState);
					break;
            }
        }

        public bool StartCountdown()
		{
            return ChangeState(HostingStates.Countdown);
        }

		public void ResendCountdown()
		{
			ChangeState(HostingStates.ResendCountdown);
		}

        public string GetGameStateText()
        {
            switch (HostingState)
			{
				case HostingStates.Adding:
				case HostingStates.AcknowledgePlayerAssignment:
					return "Adding Players";
				case HostingStates.Countdown:
					return "Countdown to Game Start";
				case HostingStates.ResendCountdown:
					return "Resending Countdown";
				case HostingStates.Playing:
					return "Game in Progress";
				case HostingStates.Summary:
					return "Debriefing Players";
				case HostingStates.GameOver:
					return "Game Over";
				default:
					return "Not In a Game";
            }
        }

	    public string GetCountdown()
	    {
			string countdown;
		    var timeRemaining = (_stateChangeTimeout - DateTime.Now).ToString(@"m\:ss");
		    switch (HostingState)
		    {
			    case HostingStates.Adding:
			    case HostingStates.AcknowledgePlayerAssignment:
				    var needed = (MinimumPlayerCount - _players.Count);
				    if (needed > 0)
				    {
					    countdown = "Waiting for " + needed + " more players";
				    }
				    else
				    {
					    countdown = "Ready to start countdown";
				    }
				    break;
			    case HostingStates.Countdown:
				    countdown = string.Format("{0} until game start", timeRemaining);
				    break;
				case HostingStates.ResendCountdown:
					countdown = string.Format("{0}", timeRemaining);
					break;
				case HostingStates.Playing:
				    countdown = string.Format("{0} until game end", timeRemaining);
				    break;
			    case HostingStates.Summary:
				    countdown = "Waiting for all players to check in";
				    break;
			    case HostingStates.GameOver:
				    countdown = "All players may now receive scores";
				    break;
			    default:
				    countdown = "Waiting";
				    break;
		    }
		    return countdown;
        }

        public bool SetPlayerName(TeamPlayerId teamPlayerId, string name)
        {
			var player = Players.Player(teamPlayerId);
            if (player == null)
			{
                Log.Add(Log.Severity.Warning, "Player not found.");
                return false;
            }

            player.Name = name;

            return true;
        }

        public void DropPlayer(TeamPlayerId teamPlayerId)
        {
			var player = Players.Player(teamPlayerId);
            if (player == null)
			{
                Log.Add(Log.Severity.Warning, "Player not found.");
                return;
            }

			var changed = false;
			switch (HostingState)
			{
				case HostingStates.Adding:
				case HostingStates.AcknowledgePlayerAssignment:
				case HostingStates.Countdown:
					_players.Remove(player.TeamPlayerId);
					changed = true;
					break;
				case HostingStates.Playing:
				case HostingStates.ResendCountdown:
				case HostingStates.Summary:
					if (player.AllTagReportsReceived()) return;
					player.Dropped = true;
					player.SurviveTime = new TimeSpan();
					player.Survived = false;
					changed = true;
					break;
				case HostingStates.Idle:
				case HostingStates.GameOver:
				default:
					Log.Add(Log.Severity.Warning, "Players cannot be dropped while in the {0} hosting state.", HostingState);
					break;
			}

	        if (changed) OnPlayerListChanged(new PlayerListChangedEventArgs(Players));
        }

        public void Update()
        {
			switch (HostingState)
			{
				case HostingStates.Idle:
					break;
				case HostingStates.Adding:
					CheckAssignPlayerFailed();

					if (DateTime.Now >= _nextAnnouncement)
					{
						var packet = PacketPacker.AnnounceGame(GameDefinition);
						Protocol.TransmitPacket(packet);

						_nextAnnouncement = DateTime.Now.AddMilliseconds(GameAnnouncementFrequencyMilliseconds);
					}
					break;
				case HostingStates.AcknowledgePlayerAssignment:
					if (DateTime.Now >= _stateChangeTimeout) ChangeState(HostingStates.Adding);
					break;
				case HostingStates.Countdown:
				case HostingStates.ResendCountdown:
					// TODO: Make ResendCountdown work better with zone games
					if (HostingState == HostingStates.ResendCountdown && DateTime.Now >= _resendCountdownPlayingStateChangeTimeout)
					{
						ChangeState(HostingStates.Summary);
					}
					else if (DateTime.Now >= _stateChangeTimeout)
					{
						ChangeState(HostingStates.Playing);
					}
					else if (DateTime.Now >= _nextAnnouncement)
					{
						var remainingSeconds = (byte)((_stateChangeTimeout - DateTime.Now).TotalSeconds);

						// There does not appear to be a reason to tell the gun the number of players
						// ahead of time.  It only prevents those players from joining midgame.  The
						// score report is bitmasked and only reports non-zero scores.
						const int playerCountTeam1 = 8;
						var playerCountTeam2 = (GameDefinition.IsTeamGame || TeamCount >= 2) ? 8 : 0;
						var playerCountTeam3 = (GameDefinition.IsTeamGame || TeamCount >= 3) ? 8 : 0;

						var packet = PacketPacker.Countdown(GameDefinition.GameId, remainingSeconds, playerCountTeam1, playerCountTeam2,
						                                    playerCountTeam3);
						Protocol.TransmitPacket(packet);

						Log.Add(Log.Severity.Information, "T-{0}", remainingSeconds);

						_nextAnnouncement = DateTime.Now.AddSeconds(1);
					}
					break;
				case HostingStates.Playing:
					if (DateTime.Now >= _stateChangeTimeout)
					{
						ChangeState(HostingStates.Summary);
					}
					else if (DateTime.Now >= _nextAnnouncement)
					{
						switch (_gameDefinition.GameType)
						{
							case GameType.OwnTheZone:
							case GameType.OwnTheZoneTwoTeams:
							case GameType.OwnTheZoneThreeTeams:
								Protocol.TransmitSignature(PacketPacker.ZoneBeacon(0, ZoneType.ContestedZone));
								_nextAnnouncement = DateTime.Now.AddMilliseconds(ZoneBeaconFrequencyMilliseconds);
								break;
							case GameType.Respawn: // RESP
							case GameType.RespawnTwoTeams: // 2TRS
							case GameType.RespawnThreeTeams: // 3TRS
								Protocol.TransmitSignature(PacketPacker.ZoneBeacon(0, ZoneType.TeamZone));
								_nextAnnouncement = DateTime.Now.AddMilliseconds(ZoneBeaconFrequencyMilliseconds);
								break;
						}
					}
					break;
				case HostingStates.Summary:
					if (DateTime.Now >= _nextAnnouncement)
					{
						var undebriefed = new List<Player>();
						foreach (var player in _players)
						{
							if (!player.AllTagReportsReceived()) undebriefed.Add(player);
						}

						Player nextDebriefPlayer = null;
						if (undebriefed.Count > 0)
						{
							_debriefPlayerSequence = _debriefPlayerSequence < Int32.MaxValue ? _debriefPlayerSequence + 1 : 0;
							nextDebriefPlayer = undebriefed[_debriefPlayerSequence%undebriefed.Count];
						}

						if (nextDebriefPlayer == null)
						{
							Log.Add(Log.Severity.Information, "All players debriefed");

							CalculateScores();
							PrintScoreReport();

							ChangeState(HostingStates.GameOver);
							break;
						}

						var packet = PacketPacker.RequestTagReport(GameDefinition.GameId, nextDebriefPlayer.TeamPlayerId);
						Protocol.TransmitPacket(packet);

						_nextAnnouncement = DateTime.Now.AddSeconds(RequestTagReportFrequencySeconds);
					}
					break;
				case HostingStates.GameOver:
					if (DateTime.Now >= _nextAnnouncement)
					{
						_nextAnnouncement = DateTime.Now.AddSeconds(GameOverAnnouncementFrequencySeconds);

						var team = Teams.Team(_rankReportTeamNumber);

						_rankReportTeamNumber++;
						var maxTeamNumber = GameDefinition.IsTeamGame ? _gameDefinition.TeamCount : 3;
						if (_rankReportTeamNumber > maxTeamNumber) _rankReportTeamNumber = 1;

						if (team == null) break;

						var playerRanks = new int[8];
						foreach (var player in team.Players)
						{
							playerRanks[player.TeamPlayerId.TeamPlayerNumber - 1] = player.Rank;
						}

						var packet = PacketPacker.RankReport(GameDefinition.GameId, team.Number, team.Rank, playerRanks);
						Protocol.TransmitPacket(packet);
					}
					break;
			}
		}

	    private void CheckAssignPlayerFailed()
	    {
		    var removeKeys = new List<UInt16>();

		    foreach (var taggerId in _joinStates.Keys)
		    {
				var joinState = _joinStates[taggerId];

			    if (!joinState.Failed)
			    {
					if (DateTime.Now < joinState.AssignPlayerSendTime.AddSeconds(AcknowledgePlayerAssignmentTimeoutSeconds)) continue;

					Log.Add(Log.Severity.Warning, "Timed out after {0} seconds waiting for AcknowledgePlayerAssignment from tagger 0x{1:X2} for game {2}.", AcknowledgePlayerAssignmentTimeoutSeconds, taggerId, GameDefinition.GameId);

					joinState.Failed = true;
				    joinState.AssignPlayerFailSendCount = 0;
					joinState.LastAssignPlayerFailSendTime = new DateTime(0);
				}

			    if (!joinState.Failed) continue;

			    if (DateTime.Now < joinState.LastAssignPlayerFailSendTime.AddMilliseconds(AssignPlayerFailedFrequencyMilliseconds)) continue;

			    var packet = PacketPacker.AssignPlayerFailed(joinState.GameId, taggerId);
				Protocol.TransmitPacket(packet);

				joinState.AssignPlayerFailSendCount++;
			    joinState.LastAssignPlayerFailSendTime = DateTime.Now;

				if (joinState.AssignPlayerFailSendCount >= AssignPlayerFailedSendCount) removeKeys.Add(taggerId);
		    }

		    foreach (var key in removeKeys)
		    {
			    var player = _joinStates[key].Player;
				Players.Remove(player.TeamPlayerId);
				_joinStates.Remove(key);
		    }

		    if (removeKeys.Count > 0)
		    {
				OnPlayerListChanged(new PlayerListChangedEventArgs(Players));
			}
		}

		private void SendPlayerAssignment(TeamPlayerId teamPlayerId)
		{
			if (HostingState != HostingStates.Adding && HostingState != HostingStates.AcknowledgePlayerAssignment) return;

			var gameId = _gameDefinition.GameId;
			var player = Players.Player(teamPlayerId);
			var taggerId = player.TaggerId;

			var packet = PacketPacker.AssignPlayer(gameId, taggerId, teamPlayerId);
			Protocol.TransmitPacket(packet);
		}
		
		#region Events
	    public class PlayerListChangedEventArgs : EventArgs
	    {
		    public PlayerListChangedEventArgs(IEnumerable<Player> players)
		    {
			    Players = players;
		    }

		    public IEnumerable<Player> Players { get; set; }
	    }

		public event EventHandler<PlayerListChangedEventArgs> PlayerListChanged;

	    protected virtual void OnPlayerListChanged(PlayerListChangedEventArgs e)
	    {
		    if (PlayerListChanged != null) PlayerListChanged(this, e);
	    }

	    public class HostingStateChangedEventArgs : EventArgs
	    {
		    public HostingStateChangedEventArgs(HostingStates previousState, HostingStates state)
		    {
			    PreviousState = previousState;
			    State = state;
		    }

		    public HostingStates PreviousState { get; set; }
		    public HostingStates State { get; set; }
	    }

	    public event EventHandler<HostingStateChangedEventArgs> HostingStateChanged;

	    protected virtual void OnHostingStateChanged(HostingStateChangedEventArgs e)
	    {
		    if (HostingStateChanged != null) HostingStateChanged(this, e);
	    }

	    #endregion

	    #endregion
    }
}
