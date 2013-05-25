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

        // TODO: Export only game types
        public enum CommandCode
		{
            //Game Types
            COMMAND_CODE_CUSTOM_GAME_MODE_HOST = 0x02, //Solo
            COMMAND_CODE_2TMS_GAME_MODE_HOST = 0x03,
            COMMAND_CODE_3TMS_GAME_MODE_HOST = 0x04,
            COMMAND_CODE_HIDE_AND_SEEK_GAME_MODE_HOST = 0x05,
            COMMAND_CODE_HUNT_THE_PREY_GAME_MODE_HOST = 0x06,
            COMMAND_CODE_2_KINGS_GAME_MODE_HOST = 0x07,
            COMMAND_CODE_3_KINGS_GAME_MODE_HOST = 0x08,
            COMMAND_CODE_OWN_THE_ZONE_GAME_MODE_HOST = 0x09, //Solo
            COMMAND_CODE_2TMS_OWN_THE_ZONE_GAME_MODE_HOST = 0x0A,
            COMMAND_CODE_3TMS_OWN_THE_ZONE_GAME_MODE_HOST = 0x0B,
            COMMAND_CODE_SPECIAL_GAME_DEFINITION = 0x0C,

            COMMAND_CODE_PLAYER_JOIN_GAME_REQUEST = 0x10,
            COMMAND_CODE_ACK_PLAYER_JOIN_RESPONSE = 0x01,
            COMMAND_CODE_CONFIRM_PLAY_JOIN_GAME = 0x11,

            // TODO: COMMAND_CODE_RESEND_JOIN_CONFIRMATION
            // Host: (10/10/2010 1:56:43 AM) Command: (15) SEQ:f,79,be,
            COMMAND_CODE_RESEND_JOIN_CONFIRMATION = 0x0f,

            COMMAND_CODE_COUNTDOWN_TO_GAME_START = 0x00,

            COMMAND_CODE_SCORE_ANNOUNCEMENT = 0x31,

            //Score Reports
            COMMAND_CODE_PLAYER_REPORT_SCORE = 0x40,
            COMMAND_CODE_PLAYER_HIT_BY_TEAM_1_REPORT = 0x41,
            COMMAND_CODE_PLAYER_HIT_BY_TEAM_2_REPORT = 0x42,
            COMMAND_CODE_PLAYER_HIT_BY_TEAM_3_REPORT = 0x43,

            COMMAND_CODE_GAME_OVER = 0x32,

            COMMAND_CODE_TEXT_MESSAGE = 0x80,
			COMMAND_CODE_SPECIAL_ATTACK = 0x90,
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

        private byte _gameId;
        
        private LazerTagSerial _serial;

        private const int GameAnnouncementFrequencySeconds = 3;
        private const int WaitForAdditionalPlayersTimeoutSeconds = 60;
        private const int MinimumPlayerCount = 2;
        private const int RequestTagReportFrequencySeconds = 3;
        private const int GameOverAnnouncementFrequencySeconds = 5;
        
        private GameDefinition _gameDefinition;
        private HostingState _hostingState = HostingState.Idle;
        private bool _paused;

        private ConfirmJoinState _confirmJoinState;

        private IHostChangedListener _listener;
        private DateTime _stateChangeTimeout;
        private DateTime _nextAnnouncement;
	    private int _debriefPlayerSequence;

        private readonly List<IrPacket> _incomingPacketQueue = new List<IrPacket>();

	    private void BaseGameSet(byte gameTimeMinutes, byte tags, byte reloads, byte shields, byte mega, bool teamTags, bool medicMode)
	    {
		    _gameId = (byte) (new Random().Next());
		    _gameDefinition = new GameDefinition
			    {
				    GameTimeMinutes = gameTimeMinutes,
				    Tags = tags,
				    Reloads = reloads,
				    Shields = shields,
				    Mega = mega,
				    TeamTags = teamTags,
				    MedicMode = medicMode,
				    GameId = _gameId,
			    };
	    }

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

            if (_players.Count >= TeamPlayerId.MaximumPlayerNumber) {
                HostDebugWriteLine("Cannot add player. The game is full.");
                return false;
            }

            switch (_gameDefinition.GameType)
			{
				// Solo Games
				case CommandCode.COMMAND_CODE_CUSTOM_GAME_MODE_HOST:
				case CommandCode.COMMAND_CODE_OWN_THE_ZONE_GAME_MODE_HOST:
					// Assign player to the first open player number
					for (var playerNumber = 1; playerNumber <= 24; playerNumber++)
					{
						if (_players.ContainsKey(new TeamPlayerId(playerNumber))) continue;

						assignedPlayerNumber = playerNumber;
						break;
					}
					break;
				// Team Games
				case CommandCode.COMMAND_CODE_2TMS_GAME_MODE_HOST:
				case CommandCode.COMMAND_CODE_3TMS_GAME_MODE_HOST:
				case CommandCode.COMMAND_CODE_HIDE_AND_SEEK_GAME_MODE_HOST:
				case CommandCode.COMMAND_CODE_HUNT_THE_PREY_GAME_MODE_HOST:
				case CommandCode.COMMAND_CODE_2_KINGS_GAME_MODE_HOST:
				case CommandCode.COMMAND_CODE_3_KINGS_GAME_MODE_HOST:
				case CommandCode.COMMAND_CODE_2TMS_OWN_THE_ZONE_GAME_MODE_HOST:
				case CommandCode.COMMAND_CODE_3TMS_OWN_THE_ZONE_GAME_MODE_HOST:
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

					if (smallestTeamNumber == 0) break;

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
					break;
			}

			if (assignedPlayerNumber == 0)
			{
				HostDebugWriteLine("Unable to assign a player number.");
				return false;
			}

			if(IsTeamGame())
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
            var teamHitReport = _incomingPacketQueue[8];
	        AssertUnknownBits("teamHitReport", teamHitReport, 0xf9);
            
            var gameId = gameIdPacket.Data;
	        var teamPlayerId = TeamPlayerId.FromPacked44(teamPlayerIdPacket.Data);
            
            if ((CommandCode)commandPacket.Data != CommandCode.COMMAND_CODE_PLAYER_REPORT_SCORE)
			{
                HostDebugWriteLine("Wrong command.");
                return false;
            }

			if (gameId != _gameId)
			{
                HostDebugWriteLine("Wrong game ID.");
                return false;
            }

			if (_players.ContainsKey(teamPlayerId))
			{
				var player = _players[teamPlayerId];

                player.Debriefed = true;
                    
                player.Survived = ((survivedPacket.Data & 0x1) == 1);
                player.TagsTaken = HexCodedDecimal.ToDecimal(tagsReceivedPacket.Data);
				player.ScoreReportTeamsWaiting[0] = ((teamHitReport.Data & 0x2) == 1);
				player.ScoreReportTeamsWaiting[1] = ((teamHitReport.Data & 0x4) == 1);
				player.ScoreReportTeamsWaiting[2] = ((teamHitReport.Data & 0x8) == 1);
				player.ZoneTime = new TimeSpan(0, 0, HexCodedDecimal.ToDecimal(zoneTimeMinutesPacket.Data), HexCodedDecimal.ToDecimal(zoneTimeSecondsPacket.Data));

				HostDebugWriteLine(String.Format("Debriefed player {0}.", player.TeamPlayerId));
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
            var reportTeamNumber = (int)(commandPacket.Data - CommandCode.COMMAND_CODE_PLAYER_HIT_BY_TEAM_1_REPORT + 1);
	        var teamPlayerId = TeamPlayerId.FromPacked44(taggerId.Data);

            if (gameIdPacket.Data != _gameId)
			{
                HostDebugWriteLine("Wrong game ID.");
                return false;
            }

			var player = LookupPlayer(teamPlayerId);
            if (player == null) return false;
            
            if (!player.ScoreReportTeamsWaiting[reportTeamNumber - 1])
			{
                HostDebugWriteLine("Score report already received from this player.");
                return false;
            }
            player.ScoreReportTeamsWaiting[reportTeamNumber - 1] = false;
            
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

	            if (IsTeamGame())
	            {
					HostDebugWriteLine(String.Format("Tagged by player {0}", taggedByPlayer.TeamPlayerId));
					taggedByPlayer.TaggedPlayerCounts[player.TeamPlayerId.PlayerNumber - 1] = HexCodedDecimal.ToDecimal(scorePacket.Data);
	            }
	            else
	            {
					HostDebugWriteLine(String.Format("Tagged by player {0}", taggedByPlayer.TeamPlayerId.ToString()));
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
				HostDebugWriteLine(String.Format("Player {0} (0x{1:2X})", player.TeamPlayerId, player.GameSessionTaggerId));
				if (IsTeamGame())
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
					case CommandCode.COMMAND_CODE_TEXT_MESSAGE:
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
					case CommandCode.COMMAND_CODE_SPECIAL_ATTACK:
						{
							var type = "Unknown Type";
							if (_incomingPacketQueue.Count == 6)
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

				        if ((CommandCode) commandPacket.Data != CommandCode.COMMAND_CODE_PLAYER_JOIN_GAME_REQUEST)
				        {
					        HostDebugWriteLine("Wrong command.");
					        return false;
				        }

				        if (_gameId != gameIdPacket.Data)
				        {
					        HostDebugWriteLine("Wrong game ID.");
					        return false;
				        }

				        foreach (var checkPlayer in _players.Values)
				        {
					        if (checkPlayer.GameSessionTaggerId == taggerId && !checkPlayer.Confirmed)
					        {
						        HostDebugWriteLine("Game session tagger ID collision.");
						        return false;
					        }
				        }

				        _confirmJoinState.TaggerId = (byte) taggerId;

				        /* 
						 * 0 = any
						 * 1-3 = team 1-3
						 */
				        var requestedTeam = (UInt16) (playerTeamRequestPacket.Data & 0x03);

				        var player = new Player((byte) taggerId);

				        if (!AssignTeamAndPlayer(requestedTeam, player))
				        {
					        return false;
				        }

				        _players[player.TeamPlayerId] = player;

				        var values = new[]
					        {
						        (UInt16) CommandCode.COMMAND_CODE_ACK_PLAYER_JOIN_RESPONSE,
						        _gameId, // Game ID
						        taggerId, // Tagger ID
						        player.TeamPlayerId.Packed23
					        };

				        if (gameIdPacket.Data != _gameId)
				        {
					        HostDebugWriteLine("Game ID does not match current game, discarding.");
					        return false;
				        }

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

				        if ((CommandCode) commandPacket.Data != CommandCode.COMMAND_CODE_CONFIRM_PLAY_JOIN_GAME)
				        {
					        HostDebugWriteLine("Wrong command.");
					        return false;
				        }

				        var gameId = gameIdPacket.Data;
				        var taggerId = taggerIdPacket.Data;

				        if (_gameId != gameId || _confirmJoinState.TaggerId != taggerId)
				        {
					        HostDebugWriteLine("Invalid confirmation: 0x{0:X2} != 0x{1:X2} || 0x{2:X2} != 0x{3:X2}", gameId, _gameId,
					                           taggerId, _confirmJoinState.TaggerId);
					        ChangeState(now, HostingState.Adding);
					        break;
				        }

				        var found = false;
				        foreach (var player in _players.Values)
				        {
					        if (player.GameSessionTaggerId == taggerId)
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
					        case CommandCode.COMMAND_CODE_PLAYER_REPORT_SCORE:
						        return ProcessPlayerReportScore();
					        case CommandCode.COMMAND_CODE_PLAYER_HIT_BY_TEAM_1_REPORT:
					        case CommandCode.COMMAND_CODE_PLAYER_HIT_BY_TEAM_2_REPORT:
					        case CommandCode.COMMAND_CODE_PLAYER_HIT_BY_TEAM_3_REPORT:
						        return ProcessPlayerHitByTeamReport();
				        }

				        return false;
			        }
	        }

	        return false;
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

						var teamText = teamNumber != 0 ? "solo" : string.Format("team {0}", teamNumber);

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
							tagsReceivedText = string.Format(" Player received {0} tags.", flags);
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
		    else if (bitCount == 8 && _incomingPacketQueue.Count > 0)
		    {
			    // mid sequence
				_incomingPacketQueue.Add(new IrPacket(IrPacket.PacketTypes.Data, data, bitCount));
		    }
		    else if (bitCount == 8)
		    {
			    // junk
			    HostDebugWriteLine("Unknown packet, clearing queue.");
			    _incomingPacketQueue.Clear();
		    }
		    else
		    {
				HostDebugWriteLine("Data: 0x{1:X2}, 0x{2:d}", data, bitCount);
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
				case CommandCode.COMMAND_CODE_OWN_THE_ZONE_GAME_MODE_HOST:
				case CommandCode.COMMAND_CODE_2TMS_OWN_THE_ZONE_GAME_MODE_HOST:
				case CommandCode.COMMAND_CODE_3TMS_OWN_THE_ZONE_GAME_MODE_HOST:
				{
					var rankings = new SortedList<int, Player>();
					var teamZoneTime = new[] { 0, 0, 0 };

					foreach (var player in _players.Values)
					{
						var playerZoneTimeSeconds = Convert.ToInt32(player.ZoneTime.TotalSeconds);
						player.Score = playerZoneTimeSeconds;

						var score = playerZoneTimeSeconds;
						score = (Int32.MaxValue - score) << 8 | player.GameSessionTaggerId;
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
						case CommandCode.COMMAND_CODE_2TMS_OWN_THE_ZONE_GAME_MODE_HOST:
						case CommandCode.COMMAND_CODE_3TMS_OWN_THE_ZONE_GAME_MODE_HOST:
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
				case CommandCode.COMMAND_CODE_CUSTOM_GAME_MODE_HOST:
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
						rankings.Add(-player.Score << 8 | player.GameSessionTaggerId, player);
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
				case CommandCode.COMMAND_CODE_2TMS_GAME_MODE_HOST:
				case CommandCode.COMMAND_CODE_3TMS_GAME_MODE_HOST:
				case CommandCode.COMMAND_CODE_HUNT_THE_PREY_GAME_MODE_HOST: //Untested
				case CommandCode.COMMAND_CODE_HIDE_AND_SEEK_GAME_MODE_HOST: //Untested
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
						score = score << 8 | player.GameSessionTaggerId;
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
				case CommandCode.COMMAND_CODE_2_KINGS_GAME_MODE_HOST:
				case CommandCode.COMMAND_CODE_3_KINGS_GAME_MODE_HOST:
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
			HostDebugWriteLine("Shot {0} tags as player {1}.", damage, teamPlayerId);
        }

        private void SendPlayerJoin(byte gameId, int preferredTeamNumber)
		{
            var taggerId = (UInt16)(new Random().Next() & 0xff);

			HostDebugWriteLine("Joining with tagger ID 0x{0:X2}.", taggerId);

	        UInt16[] join =
		        {
			        (UInt16) CommandCode.COMMAND_CODE_PLAYER_JOIN_GAME_REQUEST,
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

            switch (state) {
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

        public void DynamicHostMode(CommandCode gameType,
                                    byte gameTimeMinutes,
                                    byte tags,
                                    byte reloads,
                                    byte shields,
                                    byte mega,
                                    bool teamTags,
                                    bool medicMode,
                                    byte teamCount)
        {
            BaseGameSet(gameTimeMinutes,
                        tags,
                        reloads,
                        shields,
                        mega,
                        teamTags,
                        medicMode);
			_gameDefinition.GameType = gameType;
			_gameDefinition.TeamCount = teamCount;
        }

        public void Init2TeamHostMode(byte gameTimeMinutes,
                                      byte tags,
                                      byte reloads,
                                      byte shields,
                                      byte mega,
                                      bool teamTags,
                                      bool medicMode)
        {
            BaseGameSet(gameTimeMinutes,
                        tags,
                        reloads,
                        shields,
                        mega,
                        teamTags,
                        medicMode);
			_gameDefinition.GameType = CommandCode.COMMAND_CODE_2TMS_GAME_MODE_HOST;
			_gameDefinition.TeamCount = 2;
        }

        public void Init3TeamHostMode(byte gameTimeMinutes,
                                      byte tags,
                                      byte reloads,
                                      byte shields,
                                      byte mega,
                                      bool teamTags,
                                      bool medicMode)
        {
            BaseGameSet(gameTimeMinutes,
                        tags,
                        reloads,
                        shields,
                        mega,
                        teamTags,
                        medicMode);
			_gameDefinition.GameType = CommandCode.COMMAND_CODE_3TMS_GAME_MODE_HOST;
			_gameDefinition.TeamCount = 3;
        }
        
        public void InitCustomHostMode(byte gameTimeMinutes, 
                                      byte tags,
                                      byte reloads,
                                      byte shields,
                                      byte mega,
                                      bool teamTags,
                                      bool medicMode)
        {
            BaseGameSet(gameTimeMinutes,
                        tags,
                        reloads,
                        shields,
                        mega,
                        teamTags,
                        medicMode);
			_gameDefinition.GameType = CommandCode.COMMAND_CODE_CUSTOM_GAME_MODE_HOST;
			_gameDefinition.TeamCount = 1;
        }

		public Player LookupPlayer(TeamPlayerId teamPlayerId)
		{
			return _players.ContainsKey(teamPlayerId) ? _players[teamPlayerId] : null;
		}

        public void StartServer()
		{
            if (_hostingState != HostingState.Idle) return;

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

            player.PlayerName = name;

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
					        if (IsTeamGame())
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

					        _gameDefinition.GameId = _gameId;
					        _gameDefinition.ExtendedTagging = false;
					        _gameDefinition.RapidTags = false;
					        _gameDefinition.Hunt = false;
					        _gameDefinition.HuntDirection = false;

					        _gameDefinition.Zones = false;
					        _gameDefinition.TeamZones = false;
					        _gameDefinition.NeutralizeTaggedPlayers = false;
					        _gameDefinition.ZonesRevivePlayers = false;
					        _gameDefinition.HospitalZones = false;
					        _gameDefinition.ZonesTagPlayers = false;

					        _gameDefinition.Name = null;

					        switch (_gameDefinition.GameType)
					        {
						        case CommandCode.COMMAND_CODE_CUSTOM_GAME_MODE_HOST:
						        case CommandCode.COMMAND_CODE_2TMS_GAME_MODE_HOST:
						        case CommandCode.COMMAND_CODE_3TMS_GAME_MODE_HOST:
						        case CommandCode.COMMAND_CODE_2_KINGS_GAME_MODE_HOST:
						        case CommandCode.COMMAND_CODE_3_KINGS_GAME_MODE_HOST:
							        break;
						        case CommandCode.COMMAND_CODE_OWN_THE_ZONE_GAME_MODE_HOST:
						        case CommandCode.COMMAND_CODE_2TMS_OWN_THE_ZONE_GAME_MODE_HOST:
						        case CommandCode.COMMAND_CODE_3TMS_OWN_THE_ZONE_GAME_MODE_HOST:
							        _gameDefinition.Zones = true;
							        _gameDefinition.NeutralizeTaggedPlayers = true;
							        _gameDefinition.TeamTags = true;
							        _gameDefinition.MedicMode = true;
							        break;
						        case CommandCode.COMMAND_CODE_HIDE_AND_SEEK_GAME_MODE_HOST:
						        case CommandCode.COMMAND_CODE_HUNT_THE_PREY_GAME_MODE_HOST:
							        _gameDefinition.Hunt = true;
							        break;
					        }

					        var values = PacketPacker.GameDefinition(_gameDefinition);

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
							        (UInt16) CommandCode.COMMAND_CODE_COUNTDOWN_TO_GAME_START,
							        _gameId, // Game ID
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
						        case CommandCode.COMMAND_CODE_OWN_THE_ZONE_GAME_MODE_HOST:
						        case CommandCode.COMMAND_CODE_2TMS_OWN_THE_ZONE_GAME_MODE_HOST:
						        case CommandCode.COMMAND_CODE_3TMS_OWN_THE_ZONE_GAME_MODE_HOST:
							        TransmitBeaconBytes(0x02, 5);
									_nextAnnouncement = now.AddMilliseconds(500);
							        break;
					        }

					        // Keep sending out a countdown for taggers that may have missed it
					        var values = new UInt16[]
						        {
							        (UInt16) CommandCode.COMMAND_CODE_COUNTDOWN_TO_GAME_START,
							        _gameId, //Game ID
							        HexCodedDecimal.FromDecimal((byte) (((_stateChangeTimeout - now).Seconds%5) + 1)),
							        0x08, //players on team 1
							        0x08, //players on team 2
							        0x08  //players on team 3
						        };
					        TransmitPacket(values);
					        if (_nextAnnouncement < now || _nextAnnouncement > now.AddMilliseconds(1000))
					        {
						        _nextAnnouncement = now.AddMilliseconds(1000);
					        }
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
						        if (!player.HasBeenDebriefed()) undebriefed.Add(player);
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
							        (UInt16) CommandCode.COMMAND_CODE_SCORE_ANNOUNCEMENT,
							        _gameId, // Game ID
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

					        foreach (var player in _players.Values)
					        {
						        var values = new UInt16[]
							        {
								        (UInt16) CommandCode.COMMAND_CODE_GAME_OVER,
								        _gameId, // Game ID
								        player.TeamPlayerId.Packed44, // player index [ 4 bits - team ] [ 4 bits - player number ]
								        (UInt16) player.Rank, // player rank (not decimal hex, 1-player_count)
								        (UInt16) player.TeamRank, // team rank?
								        0x00, // unknown...
								        0x00,
								        0x00,
								        0x00,
								        0x00
							        };
						        TransmitPacket(values);
					        }
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

        public bool IsTeamGame()
		{
            switch (_gameDefinition.GameType)
			{
				case CommandCode.COMMAND_CODE_CUSTOM_GAME_MODE_HOST:
				case CommandCode.COMMAND_CODE_OWN_THE_ZONE_GAME_MODE_HOST:
					return false;
				case CommandCode.COMMAND_CODE_2TMS_GAME_MODE_HOST:
				case CommandCode.COMMAND_CODE_3TMS_GAME_MODE_HOST:
				case CommandCode.COMMAND_CODE_HIDE_AND_SEEK_GAME_MODE_HOST:
				case CommandCode.COMMAND_CODE_HUNT_THE_PREY_GAME_MODE_HOST:
				case CommandCode.COMMAND_CODE_2_KINGS_GAME_MODE_HOST:
				case CommandCode.COMMAND_CODE_3_KINGS_GAME_MODE_HOST:
				case CommandCode.COMMAND_CODE_2TMS_OWN_THE_ZONE_GAME_MODE_HOST:
				case CommandCode.COMMAND_CODE_3TMS_OWN_THE_ZONE_GAME_MODE_HOST:
					return true;
				default:
					HostDebugWriteLine("Unknown game type ({0}).", (int)_gameDefinition.GameType);
					return false;
            }
        }

        public bool IsZoneGame()
		{
            switch (_gameDefinition.GameType)
			{
				case CommandCode.COMMAND_CODE_2TMS_OWN_THE_ZONE_GAME_MODE_HOST:
				case CommandCode.COMMAND_CODE_3TMS_OWN_THE_ZONE_GAME_MODE_HOST:
				case CommandCode.COMMAND_CODE_OWN_THE_ZONE_GAME_MODE_HOST:
					return true;
				default:
					return false;
            }
        }
#endregion
    }
}
