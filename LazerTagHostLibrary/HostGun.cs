using System;
using System.IO.Ports;
using System.Globalization;
using System.Collections.Generic;
using System.Collections;
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
        static private void HostDebugWriteLine(string message)
        {
			Console.WriteLine("{0}: {1}", DateTime.Now, message);
        }

		private static void HostDebugWriteLine(string format, object argument)
		{
			HostDebugWriteLine(string.Format(format, argument));
		}

		private static void HostDebugWriteLine(string format, object argument1, object argument2)
		{
			HostDebugWriteLine(string.Format(format, argument1, argument2));
		}

		private static void HostDebugWriteLine(string format, object[] arguments)
	    {
			HostDebugWriteLine(string.Format(format, arguments));
	    }

        private struct GameState {
            public CommandCode game_type;
            public byte game_time_minutes;
            public int game_start_countdown_seconds;
            public byte tags;
            public byte reloads;
            public byte shield;
            public byte mega;
            public bool team_tag;
            public bool medic_mode;
            public byte number_of_teams;
        };

        private struct ConfirmJoinState {
            public byte player_id;
        };


        public enum HostingState {
            HOSTING_STATE_IDLE,
            HOSTING_STATE_ADDING,
            HOSTING_STATE_CONFIRM_JOIN,
            HOSTING_STATE_COUNTDOWN,
            HOSTING_STATE_PLAYING,
            HOSTING_STATE_SUMMARY,
            HOSTING_STATE_GAME_OVER,
        };

        //TODO: Export only game types
        public enum CommandCode {
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
            //TODO:COMMAND_CODE_RESEND_JOIN_CONFIRMATION
            //Host: (10/10/2010 1:56:43 AM) Command: (15) SEQ:f,79,be,
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
        };

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
			get { return game_state.number_of_teams; }
	    }

        private byte game_id = 0x00;
        
        private LazerTagSerial ltz = null;
        private const int ADDING_ADVERTISEMENT_INTERVAL_SECONDS = 3;
        private const int WAIT_FOR_ADDITIONAL_PLAYERS_TIMEOUT_SECONDS = 100;
        //private const int GAME_START_COUNTDOWN_INTERVAL_SECONDS = 10;
        private const int GAME_TIME_DURATION_MINUTES = 1;
        private const int MINIMUM_PLAYER_COUNT_START = 2;
        private const int GAME_START_COUNTDOWN_ADVERTISEMENT_INTERVAL_SECONDS = 1;
        //1s breaks
        private const int GAME_DEBRIEF_ADVERTISEMENT_INTERVAL_SECONDS = 3;
        private const int GAME_OVER_ADVERTISEMENT_INTERVAL_SECONDS = 5;
        private const int INTER_PACKET_BYTE_DELAY_MILISECONDS = 100;
        private const int MAX_PLAYER_COUNT = 24;
        private bool autostart = false;
        
        //host gun state
        private GameState game_state;
        private HostingState hosting_state = HostGun.HostingState.HOSTING_STATE_IDLE;
        private bool paused;

        private ConfirmJoinState confirm_join_state;

        //loose change state
        private IHostChangedListener listener = null;
        private DateTime state_change_timeout;
        private DateTime next_announce;
	    private int _debriefPlayerSequence = 0;

#region SerialProtocol
        private List<IRPacket> incoming_packet_queue = new List<IRPacket>();
#endregion
        
        private void BaseGameSet(byte game_time_minutes, 
                                      byte tags,
                                      byte reloads,
                                      byte shields,
                                      byte mega,
                                      bool team_tag,
                                      bool medic_mode)
        {
            game_state.game_time_minutes = game_time_minutes;
            game_state.tags = tags;
            game_state.reloads = reloads;
            game_state.shield = shields;
            game_state.mega = mega;
            game_state.team_tag = team_tag;
            game_state.medic_mode = medic_mode;
            game_id = (byte)(new Random().Next());
        }

        public void SetGameStartCountdownTime(int game_start_countdown_seconds) {
            game_state.game_start_countdown_seconds = game_start_countdown_seconds;
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

            if (_players.Count >= MAX_PLAYER_COUNT) {
                HostDebugWriteLine("Cannot add player. The game is full.");
                return false;
            }

            switch (game_state.game_type)
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
					var teamPlayerCounts = new int[game_state.number_of_teams];
					var smallestTeamNumber = 0;
					var smallestTeamPlayerCount = 8;
					for (var i = 0; i < game_state.number_of_teams; i++)
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
						requestedTeam <= game_state.number_of_teams &&
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
		        HostDebugWriteLine(string.Format("Assigned player to team {0} player {1}.", assignedTeamNumber,
		                                         assignedPlayerNumber));
	        }
	        else
	        {
				newPlayer.TeamPlayerId = new TeamPlayerId(assignedPlayerNumber);
				HostDebugWriteLine(string.Format("Assigned player to player {0}.", assignedPlayerNumber));
	        }

            return true;
        }

        private void AssertUnknownBits(String name, IRPacket data, byte mask)
        {
            if (((byte)data.data & mask) != 0x00) {
                string debug = String.Format("Unknown bits set: \"{0:s}\" unknown: {1:x} data: {2:x} mask {3:x}",
                                             name,
                                             (byte)data.data & mask,
                                             (byte)data.data,
                                             mask);
                HostDebugWriteLine(debug);
            }
        }
        
        private bool ProcessPlayerReportScore()
        {
            if (incoming_packet_queue.Count != 9) {
                return false;
            }
            
            IRPacket command_packet = incoming_packet_queue[0];
            IRPacket game_id_packet = incoming_packet_queue[1];
            IRPacket player_index_packet = incoming_packet_queue[2];
            
            IRPacket damage_recv_packet = incoming_packet_queue[3]; //decimal hex
            IRPacket still_alive_packet = incoming_packet_queue[4]; //[7 bits - zero - unknown][1 bit - alive]
            AssertUnknownBits("still_alive_packet",incoming_packet_queue[4],0xfe);

            IRPacket unknown_one = incoming_packet_queue[5];
            AssertUnknownBits("unknown_one",unknown_one,0xff);
            IRPacket zone_time_minutes_packet = incoming_packet_queue[6];
            IRPacket zone_time_seconds_packet = incoming_packet_queue[7];

            
            //[4 bits - zero - unknown][1 bit - hit by t3][1 bit - hit by t2][1 bit - hit by t1][1 bit - zero - unknown]
            IRPacket team_hit_report = incoming_packet_queue[8];
            AssertUnknownBits("team_hit_report",team_hit_report,0xf9);
            
            UInt16 confirmed_game_id = game_id_packet.data;
	        var teamPlayerId = TeamPlayerId.FromPacked44(player_index_packet.data);
            
            if ((CommandCode)command_packet.data != CommandCode.COMMAND_CODE_PLAYER_REPORT_SCORE) {
                HostDebugWriteLine("Wrong command");
                return false;
            }
            
            if (game_id != confirmed_game_id) {
                HostDebugWriteLine("Wrong game id");
                return false;
            }

			if (_players.ContainsKey(teamPlayerId))
			{
				var player = _players[teamPlayerId];

                player.Debriefed = true;
                    
                player.Survived = ((still_alive_packet.data & 0x01) == 0x01);
                player.TagsTaken = HexCodedDecimal.ToDecimal(damage_recv_packet.data);
                player.ScoreReportTeamsWaiting[0] = (team_hit_report.data & 0x2) != 0;
                player.ScoreReportTeamsWaiting[1] = (team_hit_report.data & 0x4) != 0;
                player.ScoreReportTeamsWaiting[2] = (team_hit_report.data & 0x8) != 0;
				player.ZoneTime = new TimeSpan(0, 0, HexCodedDecimal.ToDecimal(zone_time_minutes_packet.data), HexCodedDecimal.ToDecimal(zone_time_seconds_packet.data));

				HostDebugWriteLine(string.Format("Debriefed player {0}", player.TeamPlayerId.ToString()));
            }
			else
			{
                HostDebugWriteLine("Unable to find player for score report");
                return false;
            }
            
            return true;
        }

        private bool ProcessPlayerHitByTeamReport()
        {
			if (incoming_packet_queue.Count <= 4) return false;
            
            var commandPacket = incoming_packet_queue[0];
            var gameIdPacket = incoming_packet_queue[1];
            var taggerId = incoming_packet_queue[2];
            var scoreBitmaskPacket = incoming_packet_queue[3];
            
            // what team do the scores relate to hits from
            var reportTeamNumber = (int)(commandPacket.data - CommandCode.COMMAND_CODE_PLAYER_HIT_BY_TEAM_1_REPORT + 1);
	        var teamPlayerId = TeamPlayerId.FromPacked44(taggerId.data);

            if (gameIdPacket.data != game_id)
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
            var mask = (byte)scoreBitmaskPacket.data;
            for (var reportPlayerNumber = 1; reportPlayerNumber <= 8; reportPlayerNumber++)
            {
	            var reportTeamPlayerId = new TeamPlayerId(reportTeamNumber, reportPlayerNumber);
                var hasScore = ((mask >> (reportPlayerNumber - 1)) & 0x1) != 0;
                if (!hasScore) continue;
                
                if (incoming_packet_queue.Count <= packetIndex)
				{
                    HostDebugWriteLine("Ran off end of score report");
                    return false;
                }
                
                var scorePacket = incoming_packet_queue[packetIndex];

				player.TaggedByPlayerCounts[reportTeamPlayerId.PlayerNumber - 1] = HexCodedDecimal.ToDecimal(scorePacket.data);
				var taggedByPlayer = LookupPlayer(reportTeamPlayerId);
                if (taggedByPlayer == null)  continue;

	            if (IsTeamGame())
	            {
					HostDebugWriteLine(String.Format("Tagged by player {0}", taggedByPlayer.TeamPlayerId));
					taggedByPlayer.TaggedPlayerCounts[player.TeamPlayerId.PlayerNumber - 1] = HexCodedDecimal.ToDecimal(scorePacket.data);
	            }
	            else
	            {
					HostDebugWriteLine(String.Format("Tagged by player {0}", taggedByPlayer.TeamPlayerId.ToString()));
	            }
                packetIndex++;
            }

            if (listener != null) listener.PlayerListChanged(_players.Values.ToList());
            
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
						HostDebugWriteLine(string.Format("\tTags taken from team {0}: {1}", teamNumber,string.Join(", ", taggedByPlayerCounts)));
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
					HostDebugWriteLine(string.Format("\tTags taken from players: {0}", string.Join(", ", taggedByPlayerCounts)));
				}
            }
        }

        private bool ProcessCommandSequence()
        {
            DateTime now = DateTime.Now;

	        {
		        IRPacket command_packet = incoming_packet_queue[0];
				if (command_packet.data == (UInt16)CommandCode.COMMAND_CODE_TEXT_MESSAGE)
				{
					var message = new StringBuilder();
					int i = 1;
					while (i < incoming_packet_queue.Count &&
                           incoming_packet_queue[i].data >= 0x20 &&
						   incoming_packet_queue[i].data <= 0x7e &&
						   incoming_packet_queue[i].number_of_bits == 8)
					{
						message.Append(Convert.ToChar(incoming_packet_queue[i].data));
						i++;
					}
					Console.WriteLine("Received Text Message: {0}", message);
				}
	        }

            switch (hosting_state) {
            case HostingState.HOSTING_STATE_IDLE:
            {
#if JOIN_PLAYERS_TEST
                if (!autostart) {
                    IRPacket command_packet = incoming_packet_queue[0];
                    IRPacket game_id_packet = incoming_packet_queue[1];



                    if (command_packet.data == (UInt16)CommandCode.COMMAND_CODE_CUSTOM_GAME_MODE_HOST) {
                        System.Threading.Thread.Sleep(100);
                        byte game_id = (byte)game_id_packet.data;
                        if (join_count < 100) {
                            join_count++;
                            if (join_count % 10 == 0) {
                                SendPlayerJoin(game_id);
                            }
                        }
                    } else if (command_packet.data == (UInt16)CommandCode.COMMAND_CODE_ACK_PLAYER_JOIN_RESPONSE) {
                        System.Threading.Thread.Sleep(100);

                        IRPacket player_id_packet = incoming_packet_queue[2];

                        UInt16[] join = {
                            (UInt16)CommandCode.COMMAND_CODE_CONFIRM_PLAY_JOIN_GAME,
                            game_id_packet.data,
                            player_id_packet.data,
                        };
                        TransmitPacket2(ref join);

                    }
                }
#endif

                return true;
            }
            case HostingState.HOSTING_STATE_ADDING:
		    {
			    if (incoming_packet_queue.Count != 4)
			    {
				    return false;
			    }

			    IRPacket command_packet = incoming_packet_queue[0];
			    IRPacket game_id_packet = incoming_packet_queue[1];
			    IRPacket gameSessionTaggerIdPacket = incoming_packet_queue[2];
			    IRPacket player_team_request_packet = incoming_packet_queue[3];
			    UInt16 gameSessionTaggerId = gameSessionTaggerIdPacket.data;

			    if ((CommandCode) command_packet.data != CommandCode.COMMAND_CODE_PLAYER_JOIN_GAME_REQUEST)
			    {
				    HostDebugWriteLine("Wrong command");
				    return false;
			    }

			    if (game_id != game_id_packet.data)
			    {
				    HostDebugWriteLine("Wrong game id");
				    return false;
			    }

			    foreach (var collisionCheckPlayer in _players.Values)
			    {
				    if (collisionCheckPlayer.GameSessionTaggerId == gameSessionTaggerId && !collisionCheckPlayer.Confirmed)
				    {
					    HostDebugWriteLine("Game session tagger ID collision.");
					    return false;
				    }
			    }

		        confirm_join_state.player_id = (byte)gameSessionTaggerId;
                
                /* 
                 * 0 = any
                 * 1-3 = team 1-3
                 */
                var requestedTeam = (UInt16)(player_team_request_packet.data & 0x03);

                var player = new Player((byte)gameSessionTaggerId);

                if (!AssignTeamAndPlayer(requestedTeam, player))
                {
                    return false;
                }

	            _players[player.TeamPlayerId] = player;
                
                var values = new []
				{
                    (UInt16)CommandCode.COMMAND_CODE_ACK_PLAYER_JOIN_RESPONSE,
                    game_id,//Game ID
                    gameSessionTaggerId,//Player ID
                    player.TeamPlayerId.Packed23, //player #
                    // [3 bits - zero - unknown][2 bits - team assignment][3 bits - player assignment]
                };
                
                if (game_id_packet.data != game_id) {
                    HostDebugWriteLine("Game id does not match current game, discarding");
                    return false;
                }
                
                string debug = String.Format("Player {0:x} found, joining", new object[] { gameSessionTaggerId });
                HostDebugWriteLine(debug);
                
                TransmitPacket2(ref values);

                incoming_packet_queue.Clear();
                
                hosting_state = HostGun.HostingState.HOSTING_STATE_CONFIRM_JOIN;
                state_change_timeout = now.AddSeconds(2);
                
                return true;
            }
            case HostingState.HOSTING_STATE_CONFIRM_JOIN:
            {
                
                if (incoming_packet_queue.Count != 3) {
                    return false;
                }
                    
                IRPacket command_packet = incoming_packet_queue[0];
                IRPacket game_id_packet = incoming_packet_queue[1];
                IRPacket player_id_packet = incoming_packet_queue[2];
                UInt16 confirmed_game_id = game_id_packet.data;
                UInt16 confirmed_player_id = player_id_packet.data;
                
                if ((CommandCode)command_packet.data != CommandCode.COMMAND_CODE_CONFIRM_PLAY_JOIN_GAME) {
                    HostDebugWriteLine("Wrong command");
                    return false;
                }
                
                if (game_id != confirmed_game_id
                    || confirm_join_state.player_id != confirmed_player_id)
                {
                    string debug = String.Format("{0:x},{1:x},{2:x},{3:x}", 
                                                 new object[] {
                        game_id,
                        confirmed_game_id,
                        confirm_join_state.player_id,
                        confirmed_player_id});
                    HostDebugWriteLine("Invalid confirmation: " + debug);
                    ChangeState(now, HostingState.HOSTING_STATE_ADDING);
                    break;
                }
                
                var found = false;
                foreach(var player in _players.Values)
				{
                    if (player.GameSessionTaggerId == confirmed_player_id)
					{
                        player.Confirmed = true;
                        found = true;
                        break;
                    }
                }
                if (found) {
                    HostDebugWriteLine("Confirmed player");
                } else {
                    HostDebugWriteLine("Unable to find player to confirm");
                    return false;
                }
                
                if (_players.Count >= MINIMUM_PLAYER_COUNT_START) {

                    state_change_timeout = now.AddSeconds(WAIT_FOR_ADDITIONAL_PLAYERS_TIMEOUT_SECONDS);
                }
                ChangeState(now, HostingState.HOSTING_STATE_ADDING);
                incoming_packet_queue.Clear();
                if (listener != null) listener.PlayerListChanged(_players.Values.ToList());
                
                return true;
            }
            case HostingState.HOSTING_STATE_SUMMARY:
            {
                IRPacket command_packet = incoming_packet_queue[0];
                switch ((CommandCode)command_packet.data)
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
            default:
                break;
            }
            return false;
        }

        private bool ProcessPacket(IRPacket.PacketType type, UInt16 data, UInt16 number_of_bits)
        {
            //DateTime now = DateTime.Now;
            
            if (type != IRPacket.PacketType.PACKET_TYPE_LTX) return false;
            
            if (number_of_bits == 9)
            {
                if ((data & 0x100) != 0) {
                    //end sequence
                    if ((data & 0xff) == LazerTagSerial.ComputeChecksum(ref incoming_packet_queue)) {
                        HostDebugWriteLine(String.Format("RX {0}: {1}",
                                                     GetCommandCodeName((CommandCode)(incoming_packet_queue[0].data)), 
                                                     SerializeCommandSequence(ref incoming_packet_queue)));
                        if (!ProcessCommandSequence()) {
                            HostDebugWriteLine("ProcessCommandSequence failed: " + SerializeCommandSequence(ref incoming_packet_queue));
                        }
                    } else {
                        HostDebugWriteLine("Invalid Checksum " + SerializeCommandSequence(ref incoming_packet_queue));
                    }
                    incoming_packet_queue.Clear();
                } else {
                    //start sequence
                    incoming_packet_queue.Add(new IRPacket(type, data, number_of_bits));
                }
            } else if (number_of_bits == 8
                       && incoming_packet_queue.Count > 0)
            {
                //mid sequence
                incoming_packet_queue.Add(new IRPacket(type, data, number_of_bits));
            } else if (number_of_bits == 8) {
                //junk
                HostDebugWriteLine("Unknown packet, clearing queue");
                incoming_packet_queue.Clear();
            } else {
                string debug = String.Format(type.ToString() + " {0:x}, {1:d}",data, number_of_bits);
                HostDebugWriteLine(debug);
            }
            
            
            return false;
        }
        
        private bool ProcessMessage(string command, string[] parameters)
        {
            if (parameters.Length != 2)
            {
                return false;
            }

            UInt16 data = UInt16.Parse(parameters[0], NumberStyles.AllowHexSpecifier);
            UInt16 number_of_bits = UInt16.Parse(parameters[1]);

            switch (command)
			{
				case "LTX":
					return ProcessPacket(IRPacket.PacketType.PACKET_TYPE_LTX, data, number_of_bits);
				case "LTTO":
					break;
				default:
					break;
            }
            
            return false;
        }

#region SerialProtocol

        private void TransmitPacket2(ref UInt16[] values)
        {
            ltz.TransmitPacket(ref values);
        }

        private void TransmitBytes(UInt16 data, UInt16 number_of_bits)
        {
            ltz.EnqueueLTX(data,number_of_bits);
        }

        private void TransmitLTTOBytes(UInt16 data, UInt16 number_of_bits)
        {
            ltz.EnqueueLTTO(data,number_of_bits);
        }

        static private string SerializeCommandSequence(ref List<IRPacket> packets)
        {
			var hexValues = new string[packets.Count];
			for (int i = 0; i < packets.Count; i++)
			{
				hexValues[i] = string.Format("0x{0:X2}", packets[i].data);
            }
			return string.Format("SEQ: {0}", string.Join(", ", hexValues));
        }

		public class HexCodedDecimal
		{
			public static int ToDecimal(int hexCodedDecimal)
			{
				return (((hexCodedDecimal >> 4) & 0xf) * 10) + (hexCodedDecimal & 0xf);
			}

			public static byte FromDecimal(byte dec)
			{
				if (dec == 0xff) return dec;
				return (byte)(((dec / 10) << 4) | (dec % 10));
			}
		}

#endregion

        private void RankPlayers()
        {
            switch (game_state.game_type)
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
						score = (int.MaxValue - score) << 8 | player.GameSessionTaggerId;
						rankings.Add(score, player);

						teamZoneTime[player.TeamPlayerId.TeamNumber - 1] += playerZoneTimeSeconds;
					}

					//Determine PlayerRanks
					//TODO: Check if this sort order needs reversing
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
					switch (game_state.game_type)
					{
						case CommandCode.COMMAND_CODE_2TMS_OWN_THE_ZONE_GAME_MODE_HOST:
						case CommandCode.COMMAND_CODE_3TMS_OWN_THE_ZONE_GAME_MODE_HOST:
						{
							for (var i = 0; i < game_state.number_of_teams; i++)
							{
								teamRank[i] = game_state.number_of_teams;
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
					var rankings = new SortedList<int, Player>();
					//for team score
					var players_alive_per_team = new int[] {0, 0, 0,};
					//for tie breaking team scores
					var team_alive_score = new int[3] {0, 0, 0,};
					var team_final_score = new int[3] {0, 0, 0,};
					//rank for each team
					var team_rank = new int[3] {0, 0, 0,};
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
						//TODO: test to make sure scoring works accurately
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
							players_alive_per_team[player.TeamPlayerId.TeamNumber - 1]++;
							team_alive_score[player.TeamPlayerId.TeamNumber - 1] += score;
						}
						//prevent duplicates
						score = score << 8 | player.GameSessionTaggerId;
						//we want high rankings out first
						rankings.Add(-score, player);
					}
                
					//Determine Team Ranks
					for (int i = 0; i < 3; i++)
					{
						HostDebugWriteLine("Team " + (i + 1) + " Had " + players_alive_per_team[i] + " Players alive");
						HostDebugWriteLine("Combined Score: " + team_alive_score[i]);
						team_final_score[i] = (players_alive_per_team[i] << 10)
											+ (team_alive_score[i] << 2);
						HostDebugWriteLine("Final: Team " + (i + 1) + " Score " + team_final_score[i]);
					}
					for (int i = 0; i < 3; i++)
					{
						team_rank[i] = 3;
						if (team_final_score[i] >= team_final_score[(i + 1) % 3]) {
							team_rank[i]--;
						}
						if (team_final_score[i] >= team_final_score[(i + 2) % 3]) {
							team_rank[i]--;
						}
						Teams.Team(i + 1).TeamRank = team_rank[i];
						HostDebugWriteLine("Team " + (i + 1) + " Rank " + team_rank[i]);
					}

					//Determine PlayerRanks
					int rank = 0;
					int last_score = 99;
					foreach(KeyValuePair<int, Player> e in rankings)
					{
						Player p = e.Value;
						if (p.Score != last_score) {
							rank++;
							last_score = p.Score;
						}
						p.Rank = rank;
						p.TeamRank = team_rank[p.TeamPlayerId.TeamNumber - 1];
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

#region TestData
        private void Shoot(int team_number, int player_number, int damage, bool hosted)
        {
            if (!hosted) return;
            
            UInt16 shot = (UInt16)(((team_number & 0x3) << 5) 
                                   | ((player_number & 0x07) << 2)
                                   | (damage & 0x2));
            TransmitBytes(shot, 7);
            string debug = String.Format("Shot: {0:d},{1:d},{2:d} 0x{3:x}", team_number, player_number, damage, shot);
            HostDebugWriteLine(debug);
        }

        private void SendPlayerJoin(byte game_id) {

            UInt16 player_id = (UInt16)(new Random().Next() & 0xff);

            HostDebugWriteLine("Joining " + player_id);

            UInt16[] join = {
                (UInt16)CommandCode.COMMAND_CODE_PLAYER_JOIN_GAME_REQUEST,
                game_id,
                player_id,
                0x09,
            };
            TransmitPacket2(ref join);


        }

	    public void SendTextMessage(string message)
	    {
			var values = PacketPacker.packTextMessage(message);
		    TransmitPacket2(ref values);
	    }
#endregion

        private bool ChangeState(DateTime now, HostingState state) {

            paused = false;
            //TODO: Clear timeouts

            switch (state) {
            case HostingState.HOSTING_STATE_IDLE:
                _players.Clear();
                break;
            case HostingState.HOSTING_STATE_COUNTDOWN:
                if (hosting_state != HostGun.HostingState.HOSTING_STATE_ADDING) return false;
                HostDebugWriteLine("Starting countdown");
                state_change_timeout = now.AddSeconds(game_state.game_start_countdown_seconds);
                break;
            case HostingState.HOSTING_STATE_ADDING:
                HostDebugWriteLine("Joining players");
                incoming_packet_queue.Clear();
                break;
            case HostingState.HOSTING_STATE_PLAYING:
                HostDebugWriteLine("Starting Game");
                state_change_timeout = now.AddMinutes(game_state.game_time_minutes);
                incoming_packet_queue.Clear();
                break;
            case HostingState.HOSTING_STATE_SUMMARY:
                HostDebugWriteLine("Debriefing");
                break;
            case HostingState.HOSTING_STATE_GAME_OVER:
                HostDebugWriteLine("Debrief Done");
                break;
            default:
                return false;
            }

            hosting_state = state;
            next_announce = now;

            if (listener != null) {
                listener.GameStateChanged(state);
            }

            return true;
        }

#region PublicInterface

        public void DynamicHostMode(CommandCode game_type,
                                    byte game_time_minutes,
                                    byte tags,
                                    byte reloads,
                                    byte shields,
                                    byte mega,
                                    bool team_tag,
                                    bool medic_mode,
                                    byte number_of_teams)
        {
            game_state.game_type = game_type;
            BaseGameSet(game_time_minutes,
                        tags,
                        reloads,
                        shields,
                        mega,
                        team_tag,
                        medic_mode);
            game_state.number_of_teams = number_of_teams;
        }

        public void Init2TeamHostMode(byte game_time_minutes,
                                      byte tags,
                                      byte reloads,
                                      byte shields,
                                      byte mega,
                                      bool team_tag,
                                      bool medic_mode)
        {
            game_state.game_type = CommandCode.COMMAND_CODE_2TMS_GAME_MODE_HOST;
            BaseGameSet(game_time_minutes,
                        tags,
                        reloads,
                        shields,
                        mega,
                        team_tag,
                        medic_mode);
            game_state.number_of_teams = 2;
        }

        public void Init3TeamHostMode(byte game_time_minutes,
                                      byte tags,
                                      byte reloads,
                                      byte shields,
                                      byte mega,
                                      bool team_tag,
                                      bool medic_mode)
        {
            game_state.game_type = CommandCode.COMMAND_CODE_3TMS_GAME_MODE_HOST;
            BaseGameSet(game_time_minutes,
                        tags,
                        reloads,
                        shields,
                        mega,
                        team_tag,
                        medic_mode);
            game_state.number_of_teams = 3;
        }
        
        public void InitCustomHostMode(byte game_time_minutes, 
                                      byte tags,
                                      byte reloads,
                                      byte shields,
                                      byte mega,
                                      bool team_tag,
                                      bool medic_mode)
        {
            game_state.game_type = CommandCode.COMMAND_CODE_CUSTOM_GAME_MODE_HOST;
            BaseGameSet(game_time_minutes,
                        tags,
                        reloads,
                        shields,
                        mega,
                        team_tag,
                        medic_mode);
            game_state.number_of_teams = 1;
        }

		public Player LookupPlayer(TeamPlayerId teamPlayerId)
		{
			return _players.ContainsKey(teamPlayerId) ? _players[teamPlayerId] : null;
		}

        public void StartServer() {
            if (hosting_state != HostGun.HostingState.HOSTING_STATE_IDLE) return;

            ChangeState(DateTime.Now, HostGun.HostingState.HOSTING_STATE_ADDING);
        }

        public void EndGame() {
            ChangeState(DateTime.Now, HostGun.HostingState.HOSTING_STATE_IDLE);

        }

        public void DelayGame(int seconds) {
            state_change_timeout.AddSeconds(seconds);
        }

        public void Pause() {
            switch (hosting_state) {
            case HostingState.HOSTING_STATE_ADDING:
                paused = true;
                break;
            default:
                HostDebugWriteLine("Pause not enabled right now");
                break;
            }
        }

        public void Next() {
            DateTime now = DateTime.Now;
            switch (hosting_state) {
            case HostingState.HOSTING_STATE_ADDING:
                ChangeState(now, HostingState.HOSTING_STATE_COUNTDOWN);
                break;
            case HostingState.HOSTING_STATE_PLAYING:
                ChangeState(now, HostingState.HOSTING_STATE_SUMMARY);
                break;
            case HostingState.HOSTING_STATE_SUMMARY:
                ChangeState(now, HostingState.HOSTING_STATE_GAME_OVER);
                break;
            default:
                HostDebugWriteLine("Next not enabled right now");
                break;
            }
        }

        public bool StartGameNow() {
            return ChangeState(DateTime.Now, HostingState.HOSTING_STATE_COUNTDOWN);
        }



        public string GetGameStateText()
        {
            switch (hosting_state) {
            case HostingState.HOSTING_STATE_ADDING:
                return "Adding Players";
            case HostingState.HOSTING_STATE_COUNTDOWN:
                return "Countdown to game start";
            case HostingState.HOSTING_STATE_PLAYING:
                return "Game in progress";
            case HostingState.HOSTING_STATE_SUMMARY:
                return "Debriefing Players";
            case HostingState.HOSTING_STATE_GAME_OVER:
                return "Game Over";
            case HostingState.HOSTING_STATE_IDLE:
            default:
                    return "Not in a game";
            }
        }

        public string GetCountdown()
        {
            DateTime now = DateTime.Now;
            string countdown;

            if (state_change_timeout < now || paused) {
                switch (hosting_state)
				{
					case HostingState.HOSTING_STATE_ADDING:
						int needed = (MINIMUM_PLAYER_COUNT_START - _players.Count);
						countdown = needed > 0 ? String.Format("Waiting for {0} more players", needed) : "Ready to start";
						break;
					case HostingState.HOSTING_STATE_SUMMARY:
						countdown = "Waiting for all players to check in";
						break;
					case HostingState.HOSTING_STATE_GAME_OVER:
						countdown = "All players may now receive scores";
						break;
					default:
						countdown = "Waiting";
		                break;
                }
            } else {
                countdown = ((int)((state_change_timeout - now).TotalSeconds)).ToString() + " seconds";

                switch (hosting_state) {
                case HostingState.HOSTING_STATE_ADDING:
                    int needed = (MINIMUM_PLAYER_COUNT_START - _players.Count);
                    if (needed > 0) {
                        countdown = "Waiting for " + needed + " more players";
                    } else {
                        countdown += " till countdown";
                    }
                    break;
                case HostingState.HOSTING_STATE_COUNTDOWN:
                    countdown += " till game start";
                    break;
                case HostingState.HOSTING_STATE_PLAYING:
                    countdown += " till game end";
                    break;
                default:
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
			if (listener != null) listener.PlayerListChanged(_players.Values.ToList());

            return false;
        }

        public void Update() {
            DateTime now = DateTime.Now;
            
            if (ltz != null) {
                string input = ltz.TryReadCommand();
                if (input != null) {
                    int command_length = input.IndexOf(':');
                    if (command_length > 0) {
                        string command = input.Substring(0,command_length);
                    
                    
                        string paramters_line = input.Substring(command_length + 2);
                        string[] paramters = paramters_line.Split(',');
                    
                        ProcessMessage(command, paramters);
                    }
                }
            }
            
            switch (hosting_state) {
            case HostingState.HOSTING_STATE_IDLE:
            {
                //TODO
                if (autostart) {
                    Init2TeamHostMode(GAME_TIME_DURATION_MINUTES,10,0xff,15,10,true,false);
                    ChangeState(now, HostingState.HOSTING_STATE_ADDING);
                }
                break;
            }
            case HostingState.HOSTING_STATE_ADDING:
            {
                if (now.CompareTo(next_announce) > 0)
                {
	                Teams.Clear();
					if (IsTeamGame())
					{
						for (var teamNumber = 1; teamNumber <= game_state.number_of_teams; teamNumber++)
						{
							Teams.Add(new Team(teamNumber));
						}
					}
					else
					{
						Teams.Add(new Team(0));
					}

                    incoming_packet_queue.Clear();

                    bool extended_tagging = false;
                    bool unlimited_ammo = game_state.reloads == 0xff;
                    bool unlimited_mega = game_state.mega == 0xff;
                    bool friendly_fire = game_state.team_tag;
                    bool medic_mode = game_state.medic_mode;
                    bool rapid_tags = false;
                    bool hunters_hunted = false;
                    bool hunters_hunted_direction = false;

                    bool zones = false;
                    bool bases_are_teams = false;
                    bool tagged_players_are_disabled = false;
                    bool base_areas_revive_players = false;
                    bool base_areas_are_hospitals = false;
                    bool base_areas_fire_at_players = false;

                    switch (game_state.game_type) {
                    case CommandCode.COMMAND_CODE_2TMS_GAME_MODE_HOST:
                    case CommandCode.COMMAND_CODE_3TMS_GAME_MODE_HOST:
                    case CommandCode.COMMAND_CODE_CUSTOM_GAME_MODE_HOST:
                    case CommandCode.COMMAND_CODE_3_KINGS_GAME_MODE_HOST:
                    case CommandCode.COMMAND_CODE_2_KINGS_GAME_MODE_HOST:
                        break;
                    case CommandCode.COMMAND_CODE_OWN_THE_ZONE_GAME_MODE_HOST:
                    case CommandCode.COMMAND_CODE_2TMS_OWN_THE_ZONE_GAME_MODE_HOST:
                    case CommandCode.COMMAND_CODE_3TMS_OWN_THE_ZONE_GAME_MODE_HOST:
                        zones = true;
                        tagged_players_are_disabled = true;
                        friendly_fire = true;
                        medic_mode = true;
                        break;
                    case CommandCode.COMMAND_CODE_HIDE_AND_SEEK_GAME_MODE_HOST:
                    case CommandCode.COMMAND_CODE_HUNT_THE_PREY_GAME_MODE_HOST:
                        hunters_hunted = true;
                        break;
                    default:
                        break;
                    }

                    UInt16[] values = PacketPacker.packGameDefinition(game_state.game_type,
                        game_id,
                        game_state.game_time_minutes,
                        game_state.tags,
                        game_state.reloads,
                        game_state.shield,
                        game_state.mega,

                        extended_tagging,
                        unlimited_ammo,
                        unlimited_mega,
                        friendly_fire,
                        medic_mode,
                        rapid_tags,
                        hunters_hunted,
                        hunters_hunted_direction,

                        zones,
                        bases_are_teams,
                        tagged_players_are_disabled,
                        base_areas_revive_players,
                        base_areas_are_hospitals,
                        base_areas_fire_at_players,

                        game_state.number_of_teams,
                        null); //new char[] {'D','U','C','K'}


                    TransmitPacket2(ref values);
                    
                    next_announce = now.AddSeconds(ADDING_ADVERTISEMENT_INTERVAL_SECONDS);
                } else if (_players.Count >= MINIMUM_PLAYER_COUNT_START
                           && now > state_change_timeout
                           && !paused)
                {
                    ChangeState(now, HostingState.HOSTING_STATE_COUNTDOWN);
                }
                break;
            }
            case HostingState.HOSTING_STATE_CONFIRM_JOIN:
            {
                if (now.CompareTo(state_change_timeout) > 0) {
                    //TODO: Use COMMAND_CODE_RESEND_JOIN_CONFIRMATION
                    HostDebugWriteLine("No confirmation on timeout");
                    ChangeState(now, HostingState.HOSTING_STATE_ADDING);
                }
                break;
            }
            case HostingState.HOSTING_STATE_COUNTDOWN:
            {
                if (state_change_timeout < now) {
                    ChangeState(now, HostingState.HOSTING_STATE_PLAYING);
                } else if (next_announce < now) {
                    next_announce = now.AddSeconds(1);
                    
                    int seconds_left = (state_change_timeout - now).Seconds;
                    /**
                     * There does not appear to be a reason to tell the gun the number of players
                     * ahead of time.  It only prevents those players from joining midgame.  The
                     * score report is bitmasked and only reports non-zero scores.
                     */
                    UInt16[] values = new UInt16[]{
                        (UInt16)CommandCode.COMMAND_CODE_COUNTDOWN_TO_GAME_START,
                        game_id,//Game ID
                        HexCodedDecimal.FromDecimal((byte)seconds_left),
                        0x08, //players on team 1
                        0x08, //players on team 2
                        0x08, //players on team 3
                    };
                    TransmitPacket2(ref values);
                    HostDebugWriteLine("T-" + seconds_left);
                }
                break;
            }
            case HostingState.HOSTING_STATE_PLAYING:
            {
                if (now > state_change_timeout)
				{
                    ChangeState(now, HostingState.HOSTING_STATE_SUMMARY);
                }
				else if (now >= next_announce)
				{
                    switch (game_state.game_type)
					{
						case CommandCode.COMMAND_CODE_OWN_THE_ZONE_GAME_MODE_HOST:
						case CommandCode.COMMAND_CODE_2TMS_OWN_THE_ZONE_GAME_MODE_HOST:
						case CommandCode.COMMAND_CODE_3TMS_OWN_THE_ZONE_GAME_MODE_HOST:
							TransmitLTTOBytes(0x02, 5);
							next_announce = now.AddMilliseconds(500);
							break;
                    }

					// Keep sending out a countdown for taggers that may have missed it
					var values = new UInt16[]
						{
							(UInt16) CommandCode.COMMAND_CODE_COUNTDOWN_TO_GAME_START,
							game_id, //Game ID
							HexCodedDecimal.FromDecimal((byte)(((state_change_timeout - now).Seconds % 5) + 1)),
							0x08, //players on team 1
							0x08, //players on team 2
							0x08, //players on team 3
						};
					TransmitPacket2(ref values);
	                if (next_announce < now || next_announce > now.AddMilliseconds(1000))
	                {
		                next_announce = now.AddMilliseconds(1000);
	                }
                }
                break;
            }
            case HostingState.HOSTING_STATE_SUMMARY:
            {
                if (now > next_announce)
				{
					var undebriefed = new List<Player>();
					foreach (var player in _players.Values)
					{
						if (!player.HasBeenDebriefed()) undebriefed.Add(player);
					}

					Player nextDebriefPlayer = null;
					if (undebriefed.Count > 0)
	                {
						_debriefPlayerSequence = _debriefPlayerSequence < int.MaxValue ? _debriefPlayerSequence + 1 : 0;
						nextDebriefPlayer = undebriefed[_debriefPlayerSequence % undebriefed.Count];
                    }

                    if (nextDebriefPlayer == null)
					{
                        HostDebugWriteLine("All players debriefed");

                        RankPlayers();
                        PrintScoreReport();

                        ChangeState(now, HostingState.HOSTING_STATE_GAME_OVER);
                        break;
                    }
                    
                    var values = new UInt16[]
					{
                        (UInt16)CommandCode.COMMAND_CODE_SCORE_ANNOUNCEMENT,
                        game_id, // Game ID
                        nextDebriefPlayer.TeamPlayerId.Packed44, // player index [ 4 bits - team ] [ 4 bits - player number ]
                        0x0F, // unknown
                    };
                    TransmitPacket2(ref values);

					next_announce = now.AddSeconds(GAME_DEBRIEF_ADVERTISEMENT_INTERVAL_SECONDS);
				}
                break;
            }
            case HostingState.HOSTING_STATE_GAME_OVER:
            {
                if (now > next_announce)
				{
                    next_announce = now.AddSeconds(GAME_OVER_ADVERTISEMENT_INTERVAL_SECONDS);
                    
                    foreach (var player in _players.Values)
					{
                        var values = new UInt16[]
						{
                            (UInt16)CommandCode.COMMAND_CODE_GAME_OVER,
                            game_id, // Game ID
                            player.TeamPlayerId.Packed44, // player index [ 4 bits - team ] [ 4 bits - player number ]
                            (UInt16)player.Rank, // player rank (not decimal hex, 1-player_count)
                            (UInt16)player.TeamRank, // team rank?
                            0x00, // unknown...
                            0x00,
                            0x00,
                            0x00,
                            0x00,
                        };
                        TransmitPacket2(ref values);
                    }
                }
                break;
            }
            }
        }

        public void AddListener(IHostChangedListener listener) {
            this.listener = listener;
        }

        public HostGun(string device, IHostChangedListener l) {
            ltz = new LazerTagSerial(device);
            this.listener = l;
        }

        public bool SetDevice(string device) {
            if (ltz != null) {
                ltz.Stop();
                ltz = null;
            }
            if (device != null) {
                try {
                    ltz = new LazerTagSerial(device);
                } catch (Exception ex) {
                    HostDebugWriteLine(ex.ToString());
                    return false;
                }
            }
            return true;
        }

        public HostingState GetGameState() {
            return hosting_state;
        }

        public bool IsTeamGame() {
            switch (game_state.game_type)
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
					HostDebugWriteLine("Unknown game type ({0}).", (int)game_state.game_type);
					return false;
            }
        }

        public bool IsZoneGame() {
            switch (game_state.game_type) {
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
