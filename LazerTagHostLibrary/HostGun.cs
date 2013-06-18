using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace LazerTagHostLibrary
{
    public interface IHostChangedListener
    {
        void PlayerListChanged(List<Player> players);
        void GameStateChanged(HostGun.HostingState state);
    }

    public class HostGun
    {
		private static void HostDebugWriteLine(string format, params object[] arguments)
	    {
			Console.WriteLine("{0}: {1}", DateTime.Now, String.Format(format, arguments));
		}

	    private class JoinState
	    {
		    public UInt16 GameId;
		    public Player Player;
		    public DateTime AssignPlayerSendTime;
		    public bool Failed;
		    public int AssignPlayerFailSendCount;
		    public DateTime LastAssignPlayerFailSendTime;
	    };

        public enum HostingState
		{
            Idle,
            Adding,
			AcknowledgePlayerAssignment,
            Countdown,
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

        private readonly LazerTagSerial _serial;

        private const int GameAnnouncementFrequencyMilliseconds = 1500;
		private const int AcknowledgePlayerAssignmentTimeoutSeconds = 2;
		private const int AssignPlayerFailedSendCount = 6;
		private const int AssignPlayerFailedFrequencyMilliseconds = 500;
		private const int WaitForAdditionalPlayersTimeoutSeconds = 120;
        private const int MinimumPlayerCount = 2;
        private const int RequestTagReportFrequencySeconds = 3;
        private const int GameOverAnnouncementFrequencySeconds = 3;
        
        private GameDefinition _gameDefinition;
	    public GameDefinition GameDefinition
	    {
		    get { return _gameDefinition; }
	    }

	    private HostingState _hostingState = HostingState.Idle;
        private bool _paused;

        private readonly Dictionary<UInt16, JoinState> _joinStates = new Dictionary<ushort, JoinState>();

        private IHostChangedListener _listener;
        private DateTime _stateChangeTimeout;
        private DateTime _nextAnnouncement;
	    private int _debriefPlayerSequence;
		private int _rankReportTeamNumber;

	    private Packet _incomingPacket;

	    public void SetGameStartCountdownTime(int countdownTimeSeconds)
		{
            _gameDefinition.CountdownTimeSeconds = countdownTimeSeconds;
        }

        private static string GetPacketTypeName(PacketType code)
        {
            Enum c = code;
            return c.ToString();
        }

	    private bool AssignTeamAndPlayer(int requestedTeam, Player newPlayer)
	    {
		    int assignedTeamNumber;
		    var assignedPlayerNumber = 0;

		    if (_players.Count >= TeamPlayerId.MaximumPlayerNumber)
		    {
			    HostDebugWriteLine("Cannot add player. The game is full.");
			    return false;
		    }

		    if (GameDefinition.IsTeamGame)
		    {
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
				    if (teamPlayerCount < smallestTeamPlayerCount)
				    {
					    smallestTeamNumber = teamNumber;
					    smallestTeamPlayerCount = teamPlayerCount;
				    }
				    teamPlayerCounts[teamNumber - 1] = teamPlayerCount;
			    }

			    if (smallestTeamNumber == 0)
			    {
					HostDebugWriteLine("All teams are full.");
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
			    HostDebugWriteLine("Unable to assign a player number.");
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
	        HostDebugWriteLine("Unknown bits set: \"{0}\", data: 0x{2:X2}, mask: 0x{3:X2}, unknown: 0x{1:X2}", name, (byte) (signature.Data & mask), (byte) signature.Data, mask);
        }
        
        private void PrintScoreReport()
        {
            foreach (var player in _players)
            {
	            HostDebugWriteLine(String.Format("{0} (0x{1:X2})", player.DisplayName, player.TaggerId));
				if (_gameDefinition.IsTeamGame)
				{
					HostDebugWriteLine(String.Format("\tPlayer Rank: {0}, Team Rank: {1}, Score: {2}", player.Rank, player.Team.Rank, player.Score));
					for (var teamNumber = 1; teamNumber <= 3; teamNumber++)
					{
						var taggedByPlayerCounts = new int[8];
						for (var playerNumber = 1; playerNumber <= 8; playerNumber++)
						{
							var teamPlayerId = new TeamPlayerId(teamNumber, playerNumber);
							taggedByPlayerCounts[playerNumber - 1] = player.TaggedByPlayerCounts[teamPlayerId.PlayerNumber - 1];
						}
						HostDebugWriteLine(String.Format("\tTags taken from team {0}: {1}", teamNumber,String.Join(", ", taggedByPlayerCounts)));
					}
				}
				else
				{
					HostDebugWriteLine(String.Format("\tPlayer Rank: {0}, Score: {1}", player.Rank, player.Score));
					var taggedByPlayerCounts = new int[24];
					for (var playerNumber = 1; playerNumber <= 24; playerNumber++)
					{
						var teamPlayerId = new TeamPlayerId(playerNumber);
						taggedByPlayerCounts[playerNumber - 1] = player.TaggedByPlayerCounts[teamPlayerId.PlayerNumber - 1];
					}
					HostDebugWriteLine(String.Format("\tTags taken from players: {0}", String.Join(", ", taggedByPlayerCounts)));
				}
            }
        }

        private bool ProcessPacket(Packet packet)
        {
	        {
		        var packetType = (PacketType) packet.PacketTypeSignature.Data;
				switch (packetType)
				{
					case PacketType.SingleTagReport:
						{
							if (packet.Data.Count != 4) break;
							var isReply = ((packet.Data[1].Data & 0x80) & (packet.Data[2].Data & 0x80)) != 0;
							var teamPlayerId1 = TeamPlayerId.FromPacked34(packet.Data[1].Data);
							var teamPlayerId2 = TeamPlayerId.FromPacked34(packet.Data[2].Data);
							var tagsReceived = packet.Data[3].Data;
							var replyText = isReply ? "replied to tag count request from" : "requested tag count from";
							HostDebugWriteLine("Player {0} {1} player {2}. Player {0} received {3} tags from player {2}.", teamPlayerId1,
							                   replyText, teamPlayerId2, tagsReceived);
							break;
						}
					case PacketType.TextMessage:
						{
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
							HostDebugWriteLine("Received Text Message: {0}", message); 
							break;
						}
					case PacketType.SpecialAttack:
						{
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
							HostDebugWriteLine("Special Attack: {0} - {1}", type, packet.ToString());
							AssertUnknownBits("Special Attack Flags 1", packet.Data[1], 0xff);
							AssertUnknownBits("Special Attack Flags 2", packet.Data[2], 0xff);
							AssertUnknownBits("Special Attack Flags 3", packet.Data[3], 0xff);
							AssertUnknownBits("Special Attack Flags 4", packet.Data[4], 0xff);

							break;
						}
				}
	        }

	        switch (_hostingState)
	        {
		        case HostingState.Idle:
			        {
				        return true;
			        }
		        case HostingState.Adding:
				case HostingState.AcknowledgePlayerAssignment:
					{
				        if (packet.Data.Count < 2) return false;

						var gameId = packet.Data[0].Data;
						var taggerId = packet.Data[1].Data;

						switch (packet.Type)
				        {
							case PacketType.RequestJoinGame:
						        var requestedTeam = (UInt16) (packet.Data[2].Data & 0x03);
						        return ProcessRequestJoinGame(gameId, taggerId, requestedTeam);
							case PacketType.AcknowledgePlayerAssignment:
								return ProcessAcknowledgePlayerAssignment(gameId, taggerId);
							default:
								HostDebugWriteLine("Wrong command.");
								return false;
						}
			        }
		        case HostingState.Summary:
			        {
						switch (packet.Type)
				        {
					        case PacketType.TagSummary:
						        return ProcessTagSummary(packet);
					        case PacketType.TeamOneTagReport:
					        case PacketType.TeamTwoTagReport:
					        case PacketType.TeamThreeTagReport:
								return ProcessTeamTagReport(packet);
				        }

				        return false;
			        }
	        }

	        return false;
        }

	    private bool ProcessRequestJoinGame(UInt16 gameId, UInt16 taggerId, UInt16 requestedTeam)
	    {
			// TODO: Handle multiple simultaneous games
			if (gameId != GameDefinition.GameId)
			{
				HostDebugWriteLine("Wrong game ID.");
				return false;
			}

			Player player = null;

			foreach (var checkPlayer in Players)
			{
				if (checkPlayer.TaggerId == taggerId)
				{
					if (checkPlayer.Confirmed)
					{
						HostDebugWriteLine("Tagger ID collision.");
						return false;
					}

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

			HostDebugWriteLine("Assigning tagger 0x{0:X2} to player {1} for game 0x{2:X2}.", taggerId,
				   player.TeamPlayerId.ToString(_gameDefinition.IsTeamGame), gameId);

		    SendPlayerAssignment(player.TeamPlayerId);

			ChangeState(DateTime.Now, HostingState.AcknowledgePlayerAssignment);

			return true;
		}

		private bool ProcessAcknowledgePlayerAssignment(UInt16 gameId, UInt16 taggerId)
		{
			// TODO: Handle multiple simultaneous games
			if (gameId != _gameDefinition.GameId)
			{
				HostDebugWriteLine("Wrong game ID.");
				return false;
			}

			JoinState joinState;
			if (!_joinStates.TryGetValue(taggerId, out joinState))
			{
				HostDebugWriteLine("Unable to find player to confirm");
				return false;
			}

			var player = joinState.Player;
			player.Confirmed = true;
			_joinStates.Remove(taggerId);

			HostDebugWriteLine("Confirmed player {0} for game 0x{1:X2}.",
			                   player.TeamPlayerId.ToString(_gameDefinition.IsTeamGame), gameId);

			if (_joinStates.Count < 1) ChangeState(DateTime.Now, HostingState.Adding);

			if (_listener != null) _listener.PlayerListChanged(Players.ToList());

			return true;
		}

		private bool ProcessTagSummary(Packet packet)
		{
			if (packet.Type != PacketType.TagSummary)
			{
				HostDebugWriteLine("Wrong command.");
				return false;
			}

			if (packet.Data.Count != 8)
			{
				return false;
			}

			var gameId = packet.Data[0].Data;
			if (gameId != _gameDefinition.GameId)
			{
				HostDebugWriteLine("Wrong game ID.");
				return false;
			}

			var teamPlayerId = TeamPlayerId.FromPacked44(packet.Data[1].Data);
			var tagsTaken = packet.Data[2].Data; // Hex Coded Decimal
			var survivedSignature = packet.Data[3];
			AssertUnknownBits("survivedSignature", survivedSignature, 0xef);
			var survived = survivedSignature.Data;

			var unknownSignature = packet.Data[4];
			AssertUnknownBits("unknownSignature", unknownSignature, 0xff);

			var zoneTimeMinutes = packet.Data[5].Data;
			var zoneTimeSeconds = packet.Data[6].Data;

			var teamTagReports = packet.Data[7];
			AssertUnknownBits("teamTagReports", teamTagReports, 0xf1);

			var player = _players.Player(teamPlayerId);
			if (player == null)
			{
				HostDebugWriteLine("Unable to find player for score report.");
				return false;
			}

			player.Survived = (survived & 0x10) == 0x10;
			player.TagsTaken = HexCodedDecimal.ToDecimal(tagsTaken);
			player.TeamTagReportsExpected[0] = (teamTagReports.Data & 0x2) != 0;
			player.TeamTagReportsExpected[1] = (teamTagReports.Data & 0x4) != 0;
			player.TeamTagReportsExpected[2] = (teamTagReports.Data & 0x8) != 0;
			player.ZoneTime = new TimeSpan(0, 0, HexCodedDecimal.ToDecimal(zoneTimeMinutes), HexCodedDecimal.ToDecimal(zoneTimeSeconds));

			player.TagSummaryReceived = true;

			HostDebugWriteLine("Received tag summary from {0}.", player.DisplayName);

			return true;
		}

		private bool ProcessTeamTagReport(Packet packet)
		{
			if (packet.Data.Count < 4) return false;

			var gameId = packet.Data[0].Data;
			if (gameId != _gameDefinition.GameId)
			{
				HostDebugWriteLine("Wrong game ID.");
				return false;
			}

			// what team do the scores relate to hits from
			var taggedByTeamNumber = (int)(packet.PacketTypeSignature.Data - PacketType.TeamOneTagReport + 1);

			var teamPlayerId = TeamPlayerId.FromPacked44(packet.Data[1].Data);

			var player = Players.Player(teamPlayerId);
			if (player == null) return false;

			if (player.TagSummaryReceived && !player.TeamTagReportsExpected[taggedByTeamNumber - 1])
			{
				HostDebugWriteLine("A tag report from player {0} for team {1} was not expected.", player.TeamPlayerId,
				                   taggedByTeamNumber);
			}

			if (player.TeamTagReportsReceived[taggedByTeamNumber - 1])
			{
				HostDebugWriteLine("A tag report from player {0} for team {1} was already received. Discarding.",
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
					HostDebugWriteLine("Ran off end of score report");
					return false;
				}

				var tagsTaken = HexCodedDecimal.ToDecimal(packet.Data[packetIndex].Data);

				player.TaggedByPlayerCounts[taggedByTeamPlayerId.PlayerNumber - 1] = tagsTaken;

				var taggedByPlayer = Players.Player(taggedByTeamPlayerId);
				if (taggedByPlayer == null) continue;
				taggedByPlayer.TaggedPlayerCounts[player.TeamPlayerId.PlayerNumber - 1] = tagsTaken;

				packetIndex++;
			}

			if (_listener != null) _listener.PlayerListChanged(Players.ToList());

			return true;
		}

		private void ProcessTag(Signature signature)
		{
			var teamPlayerId = TeamPlayerId.FromPacked23((UInt16)((signature.Data >> 2) & 0x1f));
			var strength = (signature.Data & 0x3) + 1;
			var isTeamGame = GameDefinition != null && GameDefinition.IsTeamGame;
			HostDebugWriteLine("Received shot from player {0} with {1} tags.", teamPlayerId.ToString(isTeamGame), strength);
		}

	    private static void ProcessBeaconSignature(UInt16 data, UInt16 bitCount)
		{
			switch (bitCount)
			{
				case 5:
					{
						var teamNumber = (data >> 3) & 0x3;
						var tagReceived = ((data >> 2) & 0x1) != 0;
						var flags = data & 0x3;

						var teamText = teamNumber == 0 ? "solo" : string.Format("team {0}", teamNumber);

						string typeText;
						var tagsReceivedText = "";
						if (!tagReceived && flags != 0)
						{
							var zoneType = (ZoneType)flags;
							typeText = string.Format("zone beacon ({0})", zoneType);
						}
						else if (tagReceived)
						{
							typeText = "hit beacon";
							tagsReceivedText = string.Format(" Player received {0} tags.", flags + 1);
						}
						else
						{
							typeText = "beacon";
						}

						HostDebugWriteLine("Received {0} {1}.{2}", teamText, typeText, tagsReceivedText);
						break;
					}
				case 9:
					{
						var tagReceived = ((data >> 8) & 0x1) != 0;
						var shieldActive = ((data >> 7) & 0x1) != 0;
						var tagsRemaining = (data >> 5) & 0x3;
						//var flags = (data >> 2) & 0x7;
						var teamNumber = data  & 0x3;

						var teamText = teamNumber != 0 ? "solo" : string.Format("team {0}", teamNumber);
						var typeText = tagReceived ? "hit beacon" : "beacon";
						var shieldText = shieldActive ? " Shield active." : "";

						string tagsText;
						switch (tagsRemaining)
						{
							
							case 0x3:
								{
									tagsText = "50-100%";
									break;
								}
							case 0x2:
								{
									tagsText = "25-50%";
									break;
								}
							case 0x1:
								{
									tagsText = "1-25%";
									break;
								}
							default:
								{
									tagsText = "0";
									break;
								}
						}

						HostDebugWriteLine("Recieved {0} {1}. {3} tags remaining.{4}", teamText, typeText, tagsText, shieldText);
						break;
					}
			}
		}

	    private void ProcessDataSignature(UInt16 data,UInt16 bitCount)
	    {
			if (bitCount == 9)
		    {
				if ((data & 0x100) == 0) // packet type
			    {
				    _incomingPacket = new Packet
					    {
						    PacketTypeSignature = new Signature(SignatureType.PacketType, data),
					    };
			    }
			    else // checksum
			    {
					if (_incomingPacket == null)
					{
						HostDebugWriteLine("Stray checksum signature received.");
						return;
					}

					if (!(_incomingPacket.PacketTypeSignatureValid && _incomingPacket.DataValid))
					{
						HostDebugWriteLine("Checksum received for invalid packet: {0}", _incomingPacket);
						_incomingPacket = null;
						return;
					}

				    _incomingPacket.Checksum = new Signature(SignatureType.Checksum, data);

					if (_incomingPacket.ChecksumValid)
					{
						HostDebugWriteLine("RX {0}: {1}", GetPacketTypeName(_incomingPacket.Type), _incomingPacket);

						if (!ProcessPacket(_incomingPacket))
					    {
							HostDebugWriteLine("ProcessCommandSequence() failed: {0}", _incomingPacket);
					    }
				    }
				    else
					{
						HostDebugWriteLine("Invalid checksum received. {0}", _incomingPacket);
					}

					_incomingPacket = null;
			    }
		    }
			else if (bitCount == 8) // data
		    {
				if (_incomingPacket == null || !_incomingPacket.PacketTypeSignatureValid)
				{
					HostDebugWriteLine("Stray data packet received. 0x{0:X2} ({1})", data, bitCount);
					_incomingPacket = null;
					return;
				}

			    _incomingPacket.Data.Add(new Signature(SignatureType.Data, data));
		    }
			else if (bitCount == 7) // tag
			{
				_incomingPacket = null;
				ProcessTag(new Signature(SignatureType.Tag, data, bitCount));
			}
		    else
		    {
				HostDebugWriteLine("Stray data packet received. 0x{0:X2} ({1})", data, bitCount);
			}
	    }

	    private void ProcessSignature(string command, IList<string> parameters)
        {
	        if (parameters.Count != 2) return;

	        var data = UInt16.Parse(parameters[0], NumberStyles.AllowHexSpecifier);
	        var bitCount = UInt16.Parse(parameters[1]);

            switch (command)
			{
				case "LTTO":
					ProcessBeaconSignature(data, bitCount);
					break;
				case "LTX":
					ProcessDataSignature(data, bitCount);
					break;
				default:
					return;
			}
        }

#region SerialProtocol

		private void TransmitSignature(Signature signature)
		{
			switch (signature.Type)
			{
				case SignatureType.Beacon:
					_serial.Enqueue(signature.Data, signature.BitCount, true);
					break;
				case SignatureType.Tag:
					_serial.Enqueue(signature.Data, signature.BitCount);
					break;
				case SignatureType.PacketType:
					_serial.Enqueue((UInt16)(signature.Data & ~0x100), 9);
					break;
				case SignatureType.Data:
					_serial.Enqueue(signature.Data, signature.BitCount);
					break;
				case SignatureType.Checksum:
					_serial.Enqueue((UInt16)(signature.Data | 0x100), 9);
					break;
				default:
					HostDebugWriteLine("TransmitSignature() - Unknown SignatureType.");
					break;
			}
		}

		private void TransmitSignature(IEnumerable<Signature> signatures)
		{
			foreach (var signature in signatures)
			{
				TransmitSignature(signature);
			}
		}

		private void TransmitPacket(Packet packet)
		{
			HostDebugWriteLine("TX {0}: {1}", GetPacketTypeName(packet.Type), packet);
			TransmitSignature(packet.Signatures);
		}
#endregion

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
					CalculateScoresCustomLazerTag();
					break;
				case GameType.CustomLazerTagTwoTeams:
				case GameType.CustomLazerTagThreeTeams:
				case GameType.HuntThePrey:
				case GameType.HideAndSeek:
					CalculateScoresTeamGames();
					break;
				case GameType.KingsTwoTeams:
				case GameType.KingsThreeTeams:
					CalculateScoresKings();
					break;
				default:
					HostDebugWriteLine("Unble to score game type {0}.", _gameDefinition.GameType);
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

		private void CalculateScoresCustomLazerTag()
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
				HostDebugWriteLine("Team {0} had {1} surviving players.", teamNumber, teamSurvivedPlayerCounts[teamNumber - 1]);
				HostDebugWriteLine("The total score of the surviving players was {0}.", teamSurvivedPlayerScoreTotals[teamNumber - 1]);
				HostDebugWriteLine("Team {0}'s final score was {1}.", teamNumber, teamScore);
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
					HostDebugWriteLine("Team {0}'s king took {1} tags and {2}.", teamNumber, player.TagsTaken,
					                   player.Survived ? "survived" : "did not survive");
				}

				for (var teamNumber = 1; teamNumber <= teamCount; teamNumber++)
				{
					var teamKing = Players.Player(new TeamPlayerId(teamNumber, 1));
					if (teamKing == null)
					{
						HostDebugWriteLine("Could not find the king for team {0}.", teamNumber);
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

		private static byte GenerateRandomId()
		{
			return (byte)(new Random().Next() & 0xff);
		}

		private void SendTag(TeamPlayerId teamPlayerId, int damage)
        {
	        var signature = PacketPacker.Tag(teamPlayerId, damage);
			TransmitSignature(signature);
			HostDebugWriteLine("Shot {0} tags as player {1}.", damage, teamPlayerId.ToString(_gameDefinition.IsTeamGame));
        }

        private void SendRequestJoinGame(byte gameId, int preferredTeamNumber)
		{
			var taggerId = GenerateRandomId();
			var packet = PacketPacker.RequestJoinGame(gameId, taggerId, preferredTeamNumber);
			TransmitPacket(packet);
	        HostDebugWriteLine("Sending request to join game 0x{0:X2} with tagger ID 0x{1:X2}. Requesting team {2}", gameId,
	                           taggerId, preferredTeamNumber);
		}

	    public void SendTextMessage(string message)
	    {
			var packet = PacketPacker.TextMessage(message);
			TransmitPacket(packet);
	    }

        private bool ChangeState(DateTime now, HostingState state)
		{
            _paused = false;

            switch (state)
			{
				case HostingState.Idle:
					_players.Clear();
					break;
				case HostingState.Countdown:
					if (_hostingState != HostingState.Adding) return false;
					HostDebugWriteLine("Starting countdown");
					_stateChangeTimeout = now.AddSeconds(_gameDefinition.CountdownTimeSeconds);
					break;
				case HostingState.Adding:
					HostDebugWriteLine("Joining players");

					if (_hostingState != HostingState.AcknowledgePlayerAssignment)
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

					_stateChangeTimeout = now.AddSeconds(WaitForAdditionalPlayersTimeoutSeconds);

					break;
				case HostingState.AcknowledgePlayerAssignment:
					HostDebugWriteLine("Waiting for AcknowledgePlayerAssignment packet.");
					_stateChangeTimeout = now.AddSeconds(AcknowledgePlayerAssignmentTimeoutSeconds);
					break;
				case HostingState.Playing:
					HostDebugWriteLine("Starting Game");
					_stateChangeTimeout = now.AddMinutes(_gameDefinition.GameTimeMinutes);
					break;
				case HostingState.Summary:
					HostDebugWriteLine("Debriefing");
					break;
				case HostingState.GameOver:
					HostDebugWriteLine("Debrief Done");
					_rankReportTeamNumber = 1;
					break;
				default:
					return false;
            }

            _hostingState = state;
            _nextAnnouncement = now;

            if (_listener != null) {
                _listener.GameStateChanged(state);
            }

            return true;
        }

#region PublicInterface
        public void StartServer(GameDefinition gameDefinition)
		{
            if (_hostingState != HostingState.Idle) return;

			_gameDefinition = gameDefinition;
			_gameDefinition.GameId = GenerateRandomId();

            ChangeState(DateTime.Now, HostingState.Adding);
        }

        public void EndGame()
		{
            ChangeState(DateTime.Now, HostingState.Idle);
	        _stateChangeTimeout = new DateTime(0);
		}

        public void DelayGame(int seconds)
		{
			_stateChangeTimeout = _stateChangeTimeout.AddSeconds(seconds);
        }

        public void Pause()
		{
            switch (_hostingState)
			{
				case HostingState.Adding:
					_paused = true;
					break;
				default:
					HostDebugWriteLine("Pause is not enabled right now.");
					break;
            }
        }

        public void Next()
		{
            switch (_hostingState)
			{
				case HostingState.Adding:
					ChangeState(DateTime.Now, HostingState.Countdown);
					break;
				case HostingState.Playing:
					ChangeState(DateTime.Now, HostingState.Summary);
					break;
				case HostingState.Summary:
					foreach (var player in Players)
					{
						if (!player.AllTagReportsReceived()) DropPlayer(player.TeamPlayerId);
					}
					ChangeState(DateTime.Now, HostingState.GameOver);
					break;
				case HostingState.Idle:
				case HostingState.AcknowledgePlayerAssignment:
				case HostingState.Countdown:
				case HostingState.GameOver:
				default:
					HostDebugWriteLine("Next cannot be used while in the {0} hosting state.", _hostingState);
					break;
            }
        }

        public bool StartGame()
		{
            return ChangeState(DateTime.Now, HostingState.Countdown);
        }

        public string GetGameStateText()
        {
            switch (_hostingState)
			{
				case HostingState.Adding:
				case HostingState.AcknowledgePlayerAssignment:
					return "Adding Players";
				case HostingState.Countdown:
					return "Countdown to Game Start";
				case HostingState.Playing:
					return "Game in Progress";
				case HostingState.Summary:
					return "Debriefing Players";
				case HostingState.GameOver:
					return "Game Over";
				default:
					return "Not In a Game";
            }
        }

        public string GetCountdown()
        {
            string countdown;

			if (_stateChangeTimeout < DateTime.Now || _paused)
			{
                switch (_hostingState)
				{
					case HostingState.Adding:
					case HostingState.AcknowledgePlayerAssignment:
						var needed = (MinimumPlayerCount - _players.Count);
						countdown = needed > 0 ? String.Format("Waiting for {0} more players", needed) : "Ready to start";
						break;
					case HostingState.Summary:
						countdown = "Waiting for all players to check in";
						break;
					case HostingState.GameOver:
						countdown = "All players may now receive scores";
						break;
					default:
						countdown = "Waiting";
		                break;
                }
            }
			else
			{
				var timeRemaining = (_stateChangeTimeout - DateTime.Now);
				countdown = timeRemaining.ToString(@"m\:ss");

                switch (_hostingState)
				{
					case HostingState.Adding:
					case HostingState.AcknowledgePlayerAssignment:
						var needed = (MinimumPlayerCount - _players.Count);
						if (needed > 0)
						{
							countdown = "Waiting for " + needed + " more players";
						}
						else
						{
							countdown += " until countdown";
						}
						break;
					case HostingState.Countdown:
						countdown += " until game start";
						break;
					case HostingState.Playing:
						countdown += " until game end";
						break;
                }

            }
            return countdown;
        }

        public bool SetPlayerName(TeamPlayerId teamPlayerId, string name)
        {
			var player = Players.Player(teamPlayerId);
            if (player == null)
			{
                HostDebugWriteLine("Player not found.");
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
                HostDebugWriteLine("Player not found.");
                return;
            }

			switch (GetGameState())
			{
				case HostingState.Adding:
				case HostingState.AcknowledgePlayerAssignment:
				case HostingState.Countdown:
					_players.Remove(player.TeamPlayerId);
					if (_listener != null) _listener.PlayerListChanged(Players.ToList());
					break;
				case HostingState.Playing:
				case HostingState.Summary:
					if (player.AllTagReportsReceived()) return;
					player.Dropped = true;
					player.Survived = false;
					break;
				case HostingState.Idle:
				case HostingState.GameOver:
				default:
					HostDebugWriteLine("Players cannot be dropped while in the {0} hosting state.", _hostingState);
					return;
			}
        }

        public void Update()
		{
            if (_serial != null)
			{
                var input = _serial.Read();
                if (input != null)
                {
	                var parts = input.Split(new[] {':'}, 2, StringSplitOptions.RemoveEmptyEntries);
	                if (parts.Count() == 2)
					{
		                var command = parts[0].Trim();
						var parameters = parts[1].Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries);
						for (var i = 0; i < parameters.Count(); i++)
						{
							parameters[i] = parameters[i].Trim();
						}
						ProcessSignature(command, parameters);
                    }
                }
            }

			var now = DateTime.Now; // is this needed to avoid race conditions?

			switch (_hostingState)
	        {
		        case HostingState.Idle:
			        {
				        break;
			        }
		        case HostingState.Adding:
			        {
						CheckAssignPlayerFailed();
						
						if (now.CompareTo(_nextAnnouncement) > 0)
				        {
					        var packet = PacketPacker.AnnounceGame(_gameDefinition);
							TransmitPacket(packet);

					        _nextAnnouncement = now.AddMilliseconds(GameAnnouncementFrequencyMilliseconds);
				        }

				        var confirmedPlayerCount = Players.Count(player => player.Confirmed);
				        if (confirmedPlayerCount >= MinimumPlayerCount
				            && now > _stateChangeTimeout
				            && !_paused)
				        {
					        ChangeState(now, HostingState.Countdown);
				        }
				        break;
			        }
				case HostingState.AcknowledgePlayerAssignment:
			        {
						if (now > _stateChangeTimeout)
						{
							ChangeState(now, HostingState.Adding);
						}
				        break;
			        }
		        case HostingState.Countdown:
			        {
						if (_stateChangeTimeout < now)
				        {
							ChangeState(now, HostingState.Playing);
				        }
						else if (_nextAnnouncement < now)
						{
							var remainingSeconds = (byte) ((_stateChangeTimeout - now).TotalSeconds);

							// There does not appear to be a reason to tell the gun the number of players
							// ahead of time.  It only prevents those players from joining midgame.  The
							// score report is bitmasked and only reports non-zero scores.
							const int playerCountTeam1 = 8;
							var playerCountTeam2 = (GameDefinition.IsTeamGame || TeamCount >= 2) ? 8 : 0;
							var playerCountTeam3 = (GameDefinition.IsTeamGame || TeamCount >= 3) ? 8 : 0;

							var packet = PacketPacker.Countdown(GameDefinition.GameId, remainingSeconds, playerCountTeam1, playerCountTeam2,
							                                    playerCountTeam3);
					        TransmitPacket(packet);

							HostDebugWriteLine("T-{0}", remainingSeconds);

							_nextAnnouncement = now.AddSeconds(1);
						}
				        break;
			        }
		        case HostingState.Playing:
			        {
						if (now > _stateChangeTimeout)
				        {
							ChangeState(now, HostingState.Summary);
				        }
						else if (now >= _nextAnnouncement)
				        {
					        switch (_gameDefinition.GameType)
					        {
								case GameType.OwnTheZone:
								case GameType.OwnTheZoneTwoTeams:
								case GameType.OwnTheZoneThreeTeams:
							        TransmitSignature(PacketPacker.ZoneBeacon(0, ZoneType.ContestedZone));
									_nextAnnouncement = now.AddMilliseconds(500);
							        break;
								case GameType.Respawn:
									TransmitSignature(PacketPacker.ZoneBeacon(0, ZoneType.TeamZone));
									_nextAnnouncement = now.AddMilliseconds(500);
									break;
							}

							// TODO: Make this configurable and re-enable it.
							//// Keep sending out a countdown for taggers that may have missed it
							//var remainingSeconds = (byte)(((_stateChangeTimeout - now).Seconds % 5) + 1);
							//var playerCountTeam1 = 8;
							//var playerCountTeam2 = (GameDefinition.IsTeamGame || TeamCount >= 2) ? 8 : 0;
							//var playerCountTeam3 = (GameDefinition.IsTeamGame || TeamCount >= 3) ? 8 : 0;
							//var packet = PacketPacker.Countdown(GameDefinition.GameId, remainingSeconds, playerCountTeam1, playerCountTeam2,
							//                                    playerCountTeam3);
							//TransmitPacket(packet);
							//if (_nextAnnouncement < now || _nextAnnouncement > now.AddMilliseconds(1000))
							//{
							//    _nextAnnouncement = now.AddSeconds(1);
							//}
				        }
				        break;
			        }
		        case HostingState.Summary:
			        {
				        if (now > _nextAnnouncement)
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
						        HostDebugWriteLine("All players debriefed");

						        CalculateScores();
						        PrintScoreReport();

						        ChangeState(now, HostingState.GameOver);
						        break;
					        }

					        var packet = PacketPacker.RequestTagReport(GameDefinition.GameId, nextDebriefPlayer.TeamPlayerId);
							TransmitPacket(packet);

					        _nextAnnouncement = now.AddSeconds(RequestTagReportFrequencySeconds);
				        }
				        break;
			        }
		        case HostingState.GameOver:
			        {
				        if (now > _nextAnnouncement)
				        {
					        _nextAnnouncement = now.AddSeconds(GameOverAnnouncementFrequencySeconds);

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
					        TransmitPacket(packet);
				        }
				        break;
			        }
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

					HostDebugWriteLine(
						"Timed out after {0} seconds waiting for AcknowledgePlayerAssignment from tagger 0x{1:X2} for game 0x{2:X2}.",
						AcknowledgePlayerAssignmentTimeoutSeconds, taggerId, _gameDefinition.GameId);

					joinState.Failed = true;
				    joinState.AssignPlayerFailSendCount = 0;
					joinState.LastAssignPlayerFailSendTime = new DateTime(0);
				}

			    if (!joinState.Failed) continue;

			    if (DateTime.Now < joinState.LastAssignPlayerFailSendTime.AddMilliseconds(AssignPlayerFailedFrequencyMilliseconds)) continue;

			    var packet = PacketPacker.AssignPlayerFailed(joinState.GameId, taggerId);
			    TransmitPacket(packet);

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
	    }

	    public void AddListener(IHostChangedListener listener)
		{
            _listener = listener;
        }

        public HostGun(string device, IHostChangedListener listener)
		{
            _serial = new LazerTagSerial();
	        _serial.IoError += Serial_IoError;
	        _serial.Connect(device);
            _listener = listener;
		}

		public event LazerTagSerial.IoErrorEventHandler IoError;
		
		private void Serial_IoError(object sender, LazerTagSerial.IoErrorEventArgs e)
	    {
		    IoError(sender, e);
	    }

	    public bool SetDevice(string device)
		{
            _serial.Disconnect();
	        return _serial.Connect(device);
		}

        public HostingState GetGameState()
		{
            return _hostingState;
        }
#endregion

	    public void SendPlayerAssignment(TeamPlayerId teamPlayerId)
	    {
		    if (_hostingState != HostingState.Adding && _hostingState != HostingState.AcknowledgePlayerAssignment) return;

		    var gameId = _gameDefinition.GameId;
			var player = Players.Player(teamPlayerId);
			var taggerId = player.TaggerId;

			var packet = PacketPacker.AssignPlayer(gameId, taggerId, teamPlayerId);
			TransmitPacket(packet);
		}
    }
}
