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

	    private struct ConfirmJoinState {
            public byte TaggerId;
        };

        public enum HostingState {
            Idle,
            Adding,
            ConfirmJoin,
            Countdown,
            Playing,
            Summary,
            GameOver,
        };

	    public enum CommandCode
	    {
		    AnnounceGameCustomLazerTag = 0x02,
		    AnnounceGameCustomLazerTagTwoTeams = 0x03,
		    AnnounceGameCustomLazerTagThreeTeams = 0x04,
		    AnnounceGameHideAndSeek = 0x05,
		    AnnounceGameHuntThePrey = 0x06,
		    AnnounceGameKingsTwoTeams = 0x07,
		    AnnounceGameKingsThreeTeams = 0x08,
		    AnnounceGameOwnTheZone = 0x09,
		    AnnounceGameOwnTheZoneTwoTeams = 0x0A,
		    AnnounceGameOwnTheZoneThreeTeams = 0x0B,
		    AnnounceGameSpecial = 0x0C,

		    RequestJoinGame = 0x10,
		    AssignPlayer = 0x01,
		    AcknowledgePlayerAssignment = 0x11,

			// TODO: ResendAcknowledgePlayerAssignment
		    // SEQ: 0x00f, 0x79, 0xbe
			ResendAcknowledgePlayerAssignment = 0x0f,

		    AnnounceCountdown = 0x00,

		    RequestTagReport = 0x31,

		    TagSummary = 0x40,
		    TeamOneTagReport = 0x41,
		    TeamTwoTagReport = 0x42,
		    TeamThreeTagReport = 0x43,

		    AnnounceTeamPlayerRanks = 0x32,

		    SingleTagReport = 0x48,
		    TextMessage = 0x80,
		    SpecialAttack = 0x90,
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

		private readonly Dictionary<TeamPlayerId, Player> _players = new Dictionary<TeamPlayerId, Player>();
		public Dictionary<TeamPlayerId, Player> Players
	    {
		    get { return _players; }
	    }

	    public int TeamCount
	    {
			get { return _gameDefinition.TeamCount; }
	    }

        private LazerTagSerial _serial;

        private const int GameAnnouncementFrequencySeconds = 3;
        private const int WaitForAdditionalPlayersTimeoutSeconds = 60;
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

        private ConfirmJoinState _confirmJoinState;

        private IHostChangedListener _listener;
        private DateTime _stateChangeTimeout;
        private DateTime _nextAnnouncement;
	    private int _debriefPlayerSequence;
		private int _rankReportTeamNumber;

        private readonly List<IrPacket> _incomingPacketQueue = new List<IrPacket>();

	    public void SetGameStartCountdownTime(int countdownTimeSeconds)
		{
            _gameDefinition.CountdownTimeSeconds = countdownTimeSeconds;
        }

        private static string GetCommandCodeName(CommandCode code)
        {
            Enum c = code;
            return c.ToString();
        }

	    private bool AssignTeamAndPlayer(int requestedTeam, Player newPlayer)
	    {
		    var assignedTeamNumber = 0;
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
			    for (var i = 0; i < _gameDefinition.TeamCount; i++)
			    {
				    var teamPlayerCount = 0;
				    foreach (var player in _players.Values)
				    {
					    if (player.TeamPlayerId.TeamNumber == i + 1) teamPlayerCount++;
				    }
				    if (teamPlayerCount < smallestTeamPlayerCount)
				    {
					    smallestTeamNumber = i + 1;
					    smallestTeamPlayerCount = teamPlayerCount;
				    }
				    teamPlayerCounts[i] = teamPlayerCount;
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
				    if (!_players.ContainsKey(new TeamPlayerId(assignedTeamNumber, playerNumber)))
				    {
					    assignedPlayerNumber = playerNumber;
					    break;
				    }
			    }
		    }
		    else
		    {
			    // Assign player to the first open player number
			    for (var playerNumber = 1; playerNumber <= 24; playerNumber++)
			    {
				    if (_players.ContainsKey(new TeamPlayerId(playerNumber))) continue;
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
			    HostDebugWriteLine(String.Format("Assigned player to team {0} player {1}.", assignedTeamNumber,
			                                     assignedPlayerNumber));
		    }
		    else
		    {
			    newPlayer.TeamPlayerId = new TeamPlayerId(assignedPlayerNumber);
			    HostDebugWriteLine(String.Format("Assigned player to player {0}.", assignedPlayerNumber));
		    }

		    return true;
	    }

	    private static void AssertUnknownBits(String name, IrPacket data, byte mask)
        {
	        if (((byte) (data.Data & mask)) == 0) return;
	        HostDebugWriteLine("Unknown bits set: \"{0}\", data: 0x{2:X2}, mask: 0x{3:X2}, unknown: 0x{1:X2}", name, (byte) (data.Data & mask), (byte) data.Data, mask);
        }
        
        private bool ProcessPlayerReportScore()
        {
            if (_incomingPacketQueue.Count != 9)
			{
                return false;
            }
            
            var commandPacket = _incomingPacketQueue[0];
            var gameIdPacket = _incomingPacketQueue[1];
            var teamPlayerIdPacket = _incomingPacketQueue[2];
            
            var tagsReceivedPacket = _incomingPacketQueue[3]; // Hex Coded Decimal
            var survivedPacket = _incomingPacketQueue[4]; // [7 bits - zero - unknown][1 bit - alive]
			AssertUnknownBits("survivedPacket", _incomingPacketQueue[4], 0xfe);

            var unknownPacket = _incomingPacketQueue[5];
	        AssertUnknownBits("unknownPacket", unknownPacket, 0xff);

            var zoneTimeMinutesPacket = _incomingPacketQueue[6];
            var zoneTimeSecondsPacket = _incomingPacketQueue[7];
			
            // [4 bits - zero - unknown][1 bit - hit by t3][1 bit - hit by t2][1 bit - hit by t1][1 bit - zero - unknown]
            var teamTagReports = _incomingPacketQueue[8];
			AssertUnknownBits("teamTagReports", teamTagReports, 0xf1);
            
            var gameId = gameIdPacket.Data;
	        var teamPlayerId = TeamPlayerId.FromPacked44(teamPlayerIdPacket.Data);
            
            if ((CommandCode)commandPacket.Data != CommandCode.TagSummary)
			{
                HostDebugWriteLine("Wrong command.");
                return false;
            }

			if (gameId != _gameDefinition.GameId)
			{
                HostDebugWriteLine("Wrong game ID.");
                return false;
            }

			if (_players.ContainsKey(teamPlayerId))
			{
				var player = _players[teamPlayerId];

                player.Survived = ((survivedPacket.Data & 0x1) == 1);
                player.TagsTaken = HexCodedDecimal.ToDecimal(tagsReceivedPacket.Data);
				player.TeamTagReportsExpected[0] = ((teamTagReports.Data & 0x2) == 1);
				player.TeamTagReportsExpected[1] = ((teamTagReports.Data & 0x4) == 1);
				player.TeamTagReportsExpected[2] = ((teamTagReports.Data & 0x8) == 1);
				player.ZoneTime = new TimeSpan(0, 0, HexCodedDecimal.ToDecimal(zoneTimeMinutesPacket.Data), HexCodedDecimal.ToDecimal(zoneTimeSecondsPacket.Data));

				player.TagSummaryReceived = true;

				HostDebugWriteLine("Received tag summary from {0}.", player.DisplayName);
			}
			else
			{
                HostDebugWriteLine("Unable to find player for score report.");
                return false;
            }
            
            return true;
        }

        private bool ProcessPlayerHitByTeamReport()
        {
			if (_incomingPacketQueue.Count <= 4) return false;
            
            var commandPacket = _incomingPacketQueue[0];
            var gameIdPacket = _incomingPacketQueue[1];
            var taggerId = _incomingPacketQueue[2];
            var scoreBitmaskPacket = _incomingPacketQueue[3];
            
            // what team do the scores relate to hits from
            var reportTeamNumber = (int)(commandPacket.Data - CommandCode.TeamOneTagReport + 1);
	        var teamPlayerId = TeamPlayerId.FromPacked44(taggerId.Data);

			if (gameIdPacket.Data != _gameDefinition.GameId)
			{
                HostDebugWriteLine("Wrong game ID.");
                return false;
            }

			var player = LookupPlayer(teamPlayerId);
            if (player == null) return false;
            
            if (player.TagSummaryReceived && !player.TeamTagReportsExpected[reportTeamNumber - 1])
			{
                HostDebugWriteLine("A tag report from this player for this team was not expected.");
            }

			if (player.TeamTagReportsReceived[reportTeamNumber - 1])
			{
				HostDebugWriteLine("A tag report from this player for this team was already received. Discarding.");
				return false;
			}

	        player.TeamTagReportsReceived[reportTeamNumber - 1] = true;
            
            var packetIndex = 4;
            var mask = (byte)scoreBitmaskPacket.Data;
            for (var reportPlayerNumber = 1; reportPlayerNumber <= 8; reportPlayerNumber++)
            {
	            var reportTeamPlayerId = new TeamPlayerId(reportTeamNumber, reportPlayerNumber);
                var hasScore = ((mask >> (reportPlayerNumber - 1)) & 0x1) != 0;
                if (!hasScore) continue;
                
                if (_incomingPacketQueue.Count <= packetIndex)
				{
                    HostDebugWriteLine("Ran off end of score report");
                    return false;
                }
                
                var scorePacket = _incomingPacketQueue[packetIndex];

				player.TaggedByPlayerCounts[reportTeamPlayerId.PlayerNumber - 1] = HexCodedDecimal.ToDecimal(scorePacket.Data);
				var taggedByPlayer = LookupPlayer(reportTeamPlayerId);
                if (taggedByPlayer == null)  continue;

	            HostDebugWriteLine(String.Format("Tagged by {0}", player.DisplayName));
				if (_gameDefinition.IsTeamGame)
	            {
					taggedByPlayer.TaggedPlayerCounts[player.TeamPlayerId.PlayerNumber - 1] = HexCodedDecimal.ToDecimal(scorePacket.Data);
	            }
                packetIndex++;
            }

            if (_listener != null) _listener.PlayerListChanged(_players.Values.ToList());
            
            return true;
        }
        
        private void PrintScoreReport()
        {
            foreach (var player in _players.Values)
            {
	            HostDebugWriteLine(String.Format("{0} (0x{1:X2})", player.DisplayName, player.TaggerId));
				if (_gameDefinition.IsTeamGame)
				{
					HostDebugWriteLine(String.Format("\tPlayer Rank: {0}, Team Rank: {1}, Score: {2}", player.Rank, player.TeamRank, player.Score));
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

        private bool ProcessCommandSequence()
        {
            var now = DateTime.Now;

	        {
		        var commandPacket = _incomingPacketQueue[0];
				switch ((CommandCode) commandPacket.Data)
				{
					case CommandCode.SingleTagReport:
						{
							if (_incomingPacketQueue.Count != 5) break;
							var isReply = ((_incomingPacketQueue[2].Data & 0x80) & (_incomingPacketQueue[3].Data & 0x80)) != 0;
							var teamPlayerId1 = TeamPlayerId.FromPacked34(_incomingPacketQueue[2].Data);
							var teamPlayerId2 = TeamPlayerId.FromPacked34(_incomingPacketQueue[3].Data);
							var tagsReceived = _incomingPacketQueue[4].Data;
							var replyText = isReply ? "replied to tag count request from" : "requested tag count from";
							Console.WriteLine("Player {0} {1} player {2}. Player {0} received {3} tags from player {2}.", teamPlayerId1, replyText, teamPlayerId2, tagsReceived);
							break;
						}
					case CommandCode.TextMessage:
						{
							var message = new StringBuilder();
							var i = 1;
							while (i < _incomingPacketQueue.Count &&
								   _incomingPacketQueue[i].Data >= 0x20 &&
								   _incomingPacketQueue[i].Data <= 0x7e &&
								   _incomingPacketQueue[i].BitCount == 8)
							{
								message.Append(Convert.ToChar(_incomingPacketQueue[i].Data));
								i++;
							}
							Console.WriteLine("Received Text Message: {0}", message); 
							break;
						}
					case CommandCode.SpecialAttack:
						{
							var type = "Unknown Type";
							if (_incomingPacketQueue.Count == 5)
							{
								switch (_incomingPacketQueue[3].Data)
								{
									case 0x77:
										type = "EM Peacemaker";
										break;
									case 0xb1:
										type = "Talus Airstrike";
										break;
								}
							}
							Console.WriteLine("Special Attack: {0} - {1}", type, SerializeCommandSequence(_incomingPacketQueue.GetRange(0, 6)));
							AssertUnknownBits("Special Attack Byte 2", _incomingPacketQueue[1], 0xff);
							AssertUnknownBits("Special Attack Byte 3", _incomingPacketQueue[2], 0xff);
							AssertUnknownBits("Special Attack Byte 4", _incomingPacketQueue[3], 0xff);
							AssertUnknownBits("Special Attack Byte 5", _incomingPacketQueue[4], 0xff);

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
			        {
				        if (_incomingPacketQueue.Count != 4)
				        {
					        return false;
				        }

				        var commandPacket = _incomingPacketQueue[0];
				        var gameIdPacket = _incomingPacketQueue[1];
				        var taggerIdPacket = _incomingPacketQueue[2];
				        var playerTeamRequestPacket = _incomingPacketQueue[3];
				        var taggerId = taggerIdPacket.Data;

				        if ((CommandCode) commandPacket.Data != CommandCode.RequestJoinGame)
				        {
					        HostDebugWriteLine("Wrong command.");
					        return false;
				        }

				        if (gameIdPacket.Data != _gameDefinition.GameId)
				        {
					        HostDebugWriteLine("Wrong game ID.");
					        return false;
				        }

				        Player player = null;

						foreach (var checkPlayer in _players.Values)
				        {
					        if (checkPlayer.TaggerId == taggerId)
					        {
								if (checkPlayer.Confirmed)
								{
									HostDebugWriteLine("Tagger ID collision.");
									return false;
								}
								else
								{
									player = checkPlayer;
									break;
								}
					        }
				        }

				        if (player == null)
				        {
					        player = new Player(this, (byte) taggerId);

							/* 
							 * 0 = any
							 * 1-3 = team 1-3
							 */
							var requestedTeam = (UInt16)(playerTeamRequestPacket.Data & 0x03);

							if (!AssignTeamAndPlayer(requestedTeam, player)) return false;

							_players[player.TeamPlayerId] = player;
				        }

				        _confirmJoinState.TaggerId = (byte) taggerId;

				        var values = new[]
					        {
						        (UInt16) CommandCode.AssignPlayer,
						        GameDefinition.GameId, // Game ID
						        taggerId, // Tagger ID
						        player.TeamPlayerId.Packed23
					        };

				        HostDebugWriteLine("Tagger 0x{0:X2} found, joining.", taggerId);

				        TransmitPacket(values);

				        _incomingPacketQueue.Clear();

				        _hostingState = HostingState.ConfirmJoin;
				        _stateChangeTimeout = now.AddSeconds(2);

				        return true;
			        }
		        case HostingState.ConfirmJoin:
			        {
				        if (_incomingPacketQueue.Count != 3) return false;

				        var commandPacket = _incomingPacketQueue[0];
				        var gameIdPacket = _incomingPacketQueue[1];
				        var taggerIdPacket = _incomingPacketQueue[2];

				        if ((CommandCode) commandPacket.Data != CommandCode.AcknowledgePlayerAssignment)
				        {
					        HostDebugWriteLine("Wrong command.");
					        return false;
				        }

				        var gameId = gameIdPacket.Data;
				        var taggerId = taggerIdPacket.Data;

						if (gameId != GameDefinition.GameId || _confirmJoinState.TaggerId != taggerId)
				        {
							HostDebugWriteLine("Invalid confirmation: 0x{0:X2} != 0x{1:X2} || 0x{2:X2} != 0x{3:X2}", gameId, GameDefinition.GameId,
					                           taggerId, _confirmJoinState.TaggerId);
					        ChangeState(now, HostingState.Adding);
					        break;
				        }

				        var found = false;
				        foreach (var player in _players.Values)
				        {
					        if (player.TaggerId == taggerId)
					        {
						        player.Confirmed = true;
						        found = true;
						        break;
					        }
				        }

				        if (found)
				        {
					        HostDebugWriteLine("Confirmed player");
				        }
				        else
				        {
					        HostDebugWriteLine("Unable to find player to confirm");
					        return false;
				        }

				        if (_players.Count >= MinimumPlayerCount)
				        {
					        _stateChangeTimeout = now.AddSeconds(WaitForAdditionalPlayersTimeoutSeconds);
				        }

				        ChangeState(now, HostingState.Adding);
				        _incomingPacketQueue.Clear();

				        if (_listener != null) _listener.PlayerListChanged(_players.Values.ToList());

				        return true;
			        }
		        case HostingState.Summary:
			        {
				        var commandPacket = _incomingPacketQueue[0];

				        switch ((CommandCode) commandPacket.Data)
				        {
					        case CommandCode.TagSummary:
						        return ProcessPlayerReportScore();
					        case CommandCode.TeamOneTagReport:
					        case CommandCode.TeamTwoTagReport:
					        case CommandCode.TeamThreeTagReport:
						        return ProcessPlayerHitByTeamReport();
				        }

				        return false;
			        }
	        }

	        return false;
        }

		private void ProcessShot(UInt16 data)
		{
			var teamPlayerId = TeamPlayerId.FromPacked23((UInt16)((data >> 2) & 0x1f));
			var strength = (data & 0x3) + 1;
			var isTeamGame = GameDefinition != null && GameDefinition.IsTeamGame;
			HostDebugWriteLine("Received shot from player {0} with {1} tags.", teamPlayerId.ToString(isTeamGame), strength);
		}

	    private static void ProcessBeaconPacket(UInt16 data, UInt16 bitCount)
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

	    private void ProcessDataPacket(UInt16 data, UInt16 bitCount)
	    {
		    if (bitCount == 9)
		    {
			    if ((data & 0x100) == 0) // command
			    {
				    // start sequence
				    _incomingPacketQueue.Add(new IrPacket(IrPacket.PacketTypes.Data, data, bitCount));
			    }
			    else // checksum
			    {
				    if ((data & 0xff) == LazerTagSerial.ComputeChecksum(_incomingPacketQueue))
				    {
					    HostDebugWriteLine(String.Format("RX {0}: {1}",
					                                     GetCommandCodeName((CommandCode) (_incomingPacketQueue[0].Data)),
					                                     SerializeCommandSequence(_incomingPacketQueue)));
					    if (!ProcessCommandSequence())
					    {
						    HostDebugWriteLine("ProcessCommandSequence() failed: {0}", SerializeCommandSequence(_incomingPacketQueue));
					    }
				    }
				    else
				    {
					    HostDebugWriteLine("Invalid Checksum " + SerializeCommandSequence(_incomingPacketQueue));
				    }
				    _incomingPacketQueue.Clear();
			    }
		    }
			else if (bitCount == 8 && _incomingPacketQueue.Count > 0) // mid sequence
		    {
				_incomingPacketQueue.Add(new IrPacket(IrPacket.PacketTypes.Data, data, bitCount));
		    }
			else if (bitCount == 8) // junk
		    {
			    HostDebugWriteLine("Unknown packet, clearing queue.");
			    _incomingPacketQueue.Clear();
		    }
		    else if (bitCount == 7) // shot
		    {
			    ProcessShot(data);
				_incomingPacketQueue.Clear();
			}
		    else
		    {
				HostDebugWriteLine("Data: 0x{0:X2}, {1:d} bits", data, bitCount);
		    }
	    }

	    private void ProcessMessage(string command, IList<string> parameters)
        {
	        if (parameters.Count != 2) return;

	        var data = UInt16.Parse(parameters[0], NumberStyles.AllowHexSpecifier);
	        var bitCount = UInt16.Parse(parameters[1]);

            switch (command)
			{
				case "LTTO":
					ProcessBeaconPacket(data, bitCount);
					break;
				case "LTX":
					ProcessDataPacket(data, bitCount);
					break;
				default:
					return;
			}
        }

#region SerialProtocol

        private void TransmitPacket(UInt16[] values)
        {
	        _serial.TransmitPacket(values);
        }

		private void TransmitPacket(IEnumerable<IrPacket> packets)
		{
			foreach (var packet in packets)
			{
				if (packet.Type == IrPacket.PacketTypes.Beacon)
				{
					TransmitBeaconBytes(packet.Data, packet.BitCount);
				}
				else
				{
					TransmitDataBytes(packet.Data, packet.BitCount);
				}
			}
		}

        private void TransmitDataBytes(UInt16 data, UInt16 bitCount)
        {
	        _serial.EnqueueData(data, bitCount);
        }

        private void TransmitBeaconBytes(UInt16 data, UInt16 bitCount)
        {
	        _serial.EnqueueBeacon(data, bitCount);
        }

        static private string SerializeCommandSequence(IList<IrPacket> packets)
        {
			var hexValues = new string[packets.Count];
			for (var i = 0; i < packets.Count; i++)
			{
				hexValues[i] = String.Format("0x{0:X2}", packets[i].Data);
            }
			return String.Format("SEQ: {0}", String.Join(", ", hexValues));
        }
#endregion

        private void RankPlayers()
        {
            switch (_gameDefinition.GameType)
			{
				case GameType.OwnTheZone:
				case GameType.OwnTheZoneTwoTeams:
				case GameType.OwnTheZoneThreeTeams:
				{
					var rankings = new SortedList<int, Player>();
					var teamZoneTime = new[] { 0, 0, 0 };

					foreach (var player in _players.Values)
					{
						var playerZoneTimeSeconds = Convert.ToInt32(player.ZoneTime.TotalSeconds);
						player.Score = playerZoneTimeSeconds;

						var score = playerZoneTimeSeconds;
						score = (Int32.MaxValue - score) << 8 | player.TaggerId;
						rankings.Add(score, player);

						teamZoneTime[player.TeamPlayerId.TeamNumber - 1] += playerZoneTimeSeconds;
					}

					// Determine PlayerRanks
					// TODO: Check if this sort order needs reversing
					var rank = 0;
					var lastScore = 99;
					foreach(var e in rankings)
					{
						var p = e.Value;
						if (p.Score != lastScore)
						{
							rank++;
							lastScore = p.Score;
						}
						p.Rank = rank;
						p.TeamRank = 0;
					}

					//Team rank only for team games
					var teamRank = new[] { 0, 0, 0 };
					switch (_gameDefinition.GameType)
					{
						case GameType.OwnTheZoneTwoTeams:
						case GameType.OwnTheZoneThreeTeams:
						{
							for (var i = 0; i < _gameDefinition.TeamCount; i++)
							{
								teamRank[i] = _gameDefinition.TeamCount;
								if (teamZoneTime[i] >= teamZoneTime[(i + 1) % 3]) teamRank[i]--;
								if (teamZoneTime[i] >= teamZoneTime[(i + 2) % 3]) teamRank[i]--;
								Teams.Team(i + 1).TeamRank = teamRank[i];
								HostDebugWriteLine("Team {0} Rank {1}", (i + 1), teamRank[i]);
							}

							foreach (var player in _players.Values)
							{
								player.TeamRank = teamRank[player.TeamPlayerId.TeamNumber - 1];
							}

							break;
						}
					}

					break;
				}
				case GameType.CustomLazerTag:
				{
					var rankings = new SortedList<int, Player>();
					/*
					- Ranks are based on receiving 2 points per tag landed on players
					and losing 1 point for every time you’re tagged by a another player
					*/

					foreach (var player in _players.Values)
					{
						player.Score = -player.TagsTaken;
						for (int playerIndex = 0; playerIndex < 8; playerIndex++) // TODO: Fix this to work with up to 24 players
						{
							player.Score += 2 * player.TaggedByPlayerCounts[player.TeamPlayerId.PlayerNumber - 1];
						}

						//we want high rankings out first
						rankings.Add(-player.Score << 8 | player.TaggerId, player);
					}

					//Determine PlayerRanks
					int rank = 0;
					int lastScore = 99;
					foreach (KeyValuePair<int, Player> ranking in rankings)
					{
						Player player = ranking.Value;
						if (player.Score != lastScore)
						{
							rank++;
							lastScore = player.Score;
						}
						player.Rank = rank;
					}
					break;
				}
				case GameType.CustomLazerTagTwoTeams:
				case GameType.CustomLazerTagThreeTeams:
				case GameType.HuntThePrey: //Untested
				case GameType.HideAndSeek: //Untested
				{
					var rankedPlayers = new SortedList<int, Player>();
					var teamSurvivedPlayerCounts = new[] {0, 0, 0};
					var teamSurvivedPlayerScoreTotals = new [] {0, 0, 0}; // the total score of only the surviving players on each time
					var teamScores = new[] {0, 0, 0};
					var teamRanks = new[] {0, 0, 0};
					/*
					- Individual ranks are based on receiving 2 points per tag landed on players
					from other teams, and losing 1 point for every time you’re tagged by a player
					from another team. Tagging your own teammate (Team Tags) costs you 2
					points. Being tagged by your own teammates does not hurt your score.
					- Team ranks are based on which team has the most players not tagged out
					when the game ends (this gives an advantage to larger teams, so less-skilled
					players can be grouped together on a larger team to even things out). In the
					event of a tie, the TAG MASTER will attempt to break the tie based on the
					individual scores of the players on each team who did not get tagged out –
					this rewards the team with the more aggressive players that land more tags.
					Just hiding or trying to not get tagged may cost your team valuable points that
					could affect your team’s ranking.
					*/

					foreach (var player in _players.Values)
					{
						var score = -player.TagsTaken;
						// TODO: test to make sure scoring works accurately
						for (var teamNumber = 1; teamNumber <= 3; teamNumber++)
						{
							for (var playerNumber = 1; playerNumber <= 8; playerNumber++)
							{
								var teamPlayerId = new TeamPlayerId(teamNumber, playerNumber);
								if (player.TeamPlayerId.TeamNumber == teamNumber)
								{
									//Friendly fire
									score -= 2 * player.TaggedByPlayerCounts[teamPlayerId.PlayerNumber - 1];
								}
								else
								{
									score += 2 * player.TaggedByPlayerCounts[teamPlayerId.PlayerNumber - 1];
								}
							}
						}
                    
						player.Score = score;
						if (player.Survived)
						{
							teamSurvivedPlayerCounts[player.TeamPlayerId.TeamNumber - 1]++;
							teamSurvivedPlayerScoreTotals[player.TeamPlayerId.TeamNumber - 1] += score;
						}
						//prevent duplicates
						score = score << 8 | player.TaggerId;
						//we want high rankings out first
						rankedPlayers.Add(-score, player);
					}
                
					//Determine Team Ranks
					for (var i = 0; i < 3; i++)
					{
						HostDebugWriteLine("Team " + (i + 1) + " Had " + teamSurvivedPlayerCounts[i] + " Players alive");
						HostDebugWriteLine("Combined Score: " + teamSurvivedPlayerScoreTotals[i]);
						teamScores[i] = (teamSurvivedPlayerCounts[i] << 10)
											+ (teamSurvivedPlayerScoreTotals[i] << 2);
						HostDebugWriteLine("Final: Team " + (i + 1) + " Score " + teamScores[i]);
					}
					for (var i = 0; i < 3; i++)
					{
						teamRanks[i] = 3;
						if (teamScores[i] >= teamScores[(i + 1) % 3]) {
							teamRanks[i]--;
						}
						if (teamScores[i] >= teamScores[(i + 2) % 3]) {
							teamRanks[i]--;
						}
						Teams.Team(i + 1).TeamRank = teamRanks[i];
						HostDebugWriteLine("Team " + (i + 1) + " Rank " + teamRanks[i]);
					}

					//Determine PlayerRanks
					var rank = 0;
					var previousScore = 99;
					foreach(var player in rankedPlayers.Values)
					{
						if (player.Score != previousScore)
						{
							rank++;
							previousScore = player.Score;
						}
						player.Rank = rank;
						player.TeamRank = teamRanks[player.TeamPlayerId.TeamNumber - 1];
					}
                
					break;
				}
				case GameType.KingsTwoTeams:
				case GameType.KingsThreeTeams:
				{
					//TODO: Write kings score
					break;
				}
				default:
					HostDebugWriteLine("Unable to score match");
					break;
			}
        }

        private void Shoot(TeamPlayerId teamPlayerId, int damage)
        {
	        var shot = PacketPacker.Shot(teamPlayerId, damage);
	        TransmitPacket(shot);
			HostDebugWriteLine("Shot {0} tags as player {1}.", damage, teamPlayerId.ToString(_gameDefinition.IsTeamGame));
        }

        private void SendPlayerJoin(byte gameId, int preferredTeamNumber)
		{
            var taggerId = (UInt16)(new Random().Next() & 0xff);

			HostDebugWriteLine("Joining with tagger ID 0x{0:X2}.", taggerId);

	        UInt16[] join =
		        {
			        (UInt16) CommandCode.RequestJoinGame,
			        gameId,
			        taggerId,
			        (UInt16) (preferredTeamNumber & 0x3)
		        };

	        TransmitPacket(join);
		}

	    public void SendTextMessage(string message)
	    {
			var values = PacketPacker.TextMessage(message);
		    TransmitPacket(values);
	    }

        private bool ChangeState(DateTime now, HostingState state) {

            _paused = false;
            //TODO: Clear timeouts

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
					_incomingPacketQueue.Clear();
					break;
				case HostingState.Playing:
					HostDebugWriteLine("Starting Game");
					_stateChangeTimeout = now.AddMinutes(_gameDefinition.GameTimeMinutes);
					_incomingPacketQueue.Clear();
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
		public Player LookupPlayer(TeamPlayerId teamPlayerId)
		{
			return _players.ContainsKey(teamPlayerId) ? _players[teamPlayerId] : null;
		}

        public void StartServer(GameDefinition gameDefinition)
		{
            if (_hostingState != HostingState.Idle) return;

			_gameDefinition = gameDefinition;
			_gameDefinition.GameId = (byte)(new Random().Next());

            ChangeState(DateTime.Now, HostingState.Adding);
        }

        public void EndGame()
		{
            ChangeState(DateTime.Now, HostingState.Idle);
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
					ChangeState(DateTime.Now, HostingState.GameOver);
					break;
				default:
					HostDebugWriteLine("Next not enabled right now.");
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
				countdown = ((int)((_stateChangeTimeout - DateTime.Now).TotalSeconds)) + " seconds";

                switch (_hostingState)
				{
					case HostingState.Adding:
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
			var player = LookupPlayer(teamPlayerId);

            if (player == null)
			{
                HostDebugWriteLine("Player not found.");
                return false;
            }

            player.Name = name;

            return true;
        }

        public bool DropPlayer(TeamPlayerId teamPlayerId)
        {
			var player = LookupPlayer(teamPlayerId);

            if (player == null)
			{
                HostDebugWriteLine("Player not found.");
                return false;
            }

	        _players.Remove(player.TeamPlayerId);
			if (_listener != null) _listener.PlayerListChanged(_players.Values.ToList());

            return false;
        }

        public void Update() {
            if (_serial != null)
			{
                var input = _serial.TryReadCommand();
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
						ProcessMessage(command, parameters);
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
				        if (now.CompareTo(_nextAnnouncement) > 0)
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
						        Teams.Add(new Team(0));
					        }

					        _incomingPacketQueue.Clear();

					        var values = PacketPacker.AnnounceGame(_gameDefinition);
					        TransmitPacket(values);

					        _nextAnnouncement = now.AddSeconds(GameAnnouncementFrequencySeconds);
				        }
				        else if (_players.Count >= MinimumPlayerCount
				                 && now > _stateChangeTimeout
				                 && !_paused)
				        {
					        ChangeState(now, HostingState.Countdown);
				        }
				        break;
			        }
		        case HostingState.ConfirmJoin:
			        {
						if (now.CompareTo(_stateChangeTimeout) > 0)
				        {
					        //TODO: Use COMMAND_CODE_RESEND_JOIN_CONFIRMATION
					        HostDebugWriteLine("No confirmation on timeout");
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
							_nextAnnouncement = now.AddSeconds(1);

							var remainingSeconds = (byte)((_stateChangeTimeout - now).TotalSeconds);
					        /**
							 * There does not appear to be a reason to tell the gun the number of players
							 * ahead of time.  It only prevents those players from joining midgame.  The
							 * score report is bitmasked and only reports non-zero scores.
							 */
					        var values = new UInt16[]
						        {
							        (UInt16) CommandCode.AnnounceCountdown,
							        GameDefinition.GameId, // Game ID
							        HexCodedDecimal.FromDecimal(remainingSeconds),
							        0x08, // Players on Team 1
							        0x08, // Players on Team 2
							        0x08 // Players on Team 3
						        };
					        TransmitPacket(values);
					        HostDebugWriteLine("T-{0}", remainingSeconds);
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
							        TransmitPacket(PacketPacker.ZoneBeacon(0, ZoneType.ContestedZone));
									_nextAnnouncement = now.AddMilliseconds(500);
							        break;
								case GameType.Respawn:
							        TransmitPacket(PacketPacker.ZoneBeacon(0, ZoneType.TeamZone));
									_nextAnnouncement = now.AddMilliseconds(500);
									break;
							}

							// TODO: Make this configurable and re-enable it.
							//// Keep sending out a countdown for taggers that may have missed it
							//var values = new UInt16[]
							//    {
							//        (UInt16) CommandCode.AnnounceCountdown,
							//        GameDefinition.GameId, //Game ID
							//        HexCodedDecimal.FromDecimal((byte) (((_stateChangeTimeout - now).Seconds%5) + 1)),
							//        0x08, //players on team 1
							//        0x08, //players on team 2
							//        0x08  //players on team 3
							//    };
							//TransmitPacket(values);
							//if (_nextAnnouncement < now || _nextAnnouncement > now.AddMilliseconds(1000))
							//{
							//    _nextAnnouncement = now.AddMilliseconds(1000);
							//}
				        }
				        break;
			        }
		        case HostingState.Summary:
			        {
				        if (now > _nextAnnouncement)
				        {
					        var undebriefed = new List<Player>();
					        foreach (var player in _players.Values)
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

						        RankPlayers();
						        PrintScoreReport();

						        ChangeState(now, HostingState.GameOver);
						        break;
					        }

					        var values = new UInt16[]
						        {
							        (UInt16) CommandCode.RequestTagReport,
							        GameDefinition.GameId, // Game ID
							        nextDebriefPlayer.TeamPlayerId.Packed44, // player index [ 4 bits - team ] [ 4 bits - player number ]
							        0x0F // unknown
						        };
					        TransmitPacket(values);

					        _nextAnnouncement = now.AddSeconds(RequestTagReportFrequencySeconds);
				        }
				        break;
			        }
		        case HostingState.GameOver:
			        {
				        if (now > _nextAnnouncement)
				        {
					        _nextAnnouncement = now.AddSeconds(GameOverAnnouncementFrequencySeconds);

							// TODO: Fix this to work with solo games
							var team = _teams.Team(_rankReportTeamNumber);

							_rankReportTeamNumber++;
							if (_rankReportTeamNumber > _gameDefinition.TeamCount) _rankReportTeamNumber = 1;

					        if (team == null) break;

							var values = new UInt16[11];
						    values[0] = (UInt16) CommandCode.AnnounceTeamPlayerRanks;
							values[1] = GameDefinition.GameId;
						    values[2] = (UInt16) (((team.TeamNumber & 0xf) << 4) | (team.TeamRank & 0xf));

						    foreach (var player in _players.Values)
						    {
							    if (player.TeamPlayerId.TeamNumber != team.TeamNumber) continue;
							    values[player.TeamPlayerId.TeamPlayerNumber + 2] = (UInt16) player.Rank;
						    }

						    TransmitPacket(values);
				        }
				        break;
			        }
	        }
        }

        public void AddListener(IHostChangedListener listener)
		{
            _listener = listener;
        }

        public HostGun(string device, IHostChangedListener l)
		{
            _serial = new LazerTagSerial(device);
            _listener = l;
		}

        public bool SetDevice(string device)
		{
            if (_serial != null)
			{
                _serial.Stop();
                _serial = null;
            }
            if (device != null)
			{
                try
				{
                    _serial = new LazerTagSerial(device);
                }
				catch (Exception ex)
				{
                    HostDebugWriteLine(ex.ToString());
                    return false;
                }
            }
            return true;
        }

        public HostingState GetGameState()
		{
            return _hostingState;
        }
#endregion
    }
}
