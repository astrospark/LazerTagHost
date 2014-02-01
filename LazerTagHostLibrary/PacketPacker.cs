using System;

namespace LazerTagHostLibrary
{
    public class PacketPacker
    {
		public static Signature Tag(TeamPlayerId teamPlayerId, int damage)
		{
			var flags = (byte) ((teamPlayerId.Packed23 << 2) | (damage & 0x3));
			return new Signature(SignatureType.Tag, flags, 7);
		}

		public static Signature Tag(int damage)
		{
			return Tag(new TeamPlayerId(0), damage);
		}

	    public static Signature HostileZoneTag(int teamNumber, int damage = 1)
	    {
		    var flags = (byte) (((teamNumber%4) + 4) << 2 | (damage & 0x3));
		    return new Signature(SignatureType.Tag, flags, 7);
	    }

	    public static Signature Beacon(int teamNumber, bool tagReceived, int tagStrength)
        {
	        if (!tagReceived) tagStrength = 0;
	        
			var flags =
		        (byte) (((teamNumber & 0x3) << 3) |
		                ((tagReceived ? 1 : 0) << 2) |
		                ((tagStrength & 0x3)));
	        
			return new Signature(SignatureType.Beacon, flags, 5);
        }

		public static Signature ZoneBeacon(int teamNumber, HostGun.ZoneType zoneType)
		{
			var flags = (byte) (((teamNumber & 0x3) << 3) | ((byte) zoneType & 0x3));
			return new Signature(SignatureType.Beacon, flags, 5);
		}

        public static Packet AnnounceGame(GameDefinition gameDefinition)
        {
	        if (gameDefinition.GameTypeInfo.PacketType == PacketType.AnnounceGameSpecial)
		        return AnnounceSpecialGame(gameDefinition);

			var packet = new Packet();
			packet.Type = gameDefinition.GameTypeInfo.PacketType;
			packet.Data.Add(new Signature(SignatureType.Data, gameDefinition.GameId));
			packet.Data.Add(new Signature(SignatureType.Data, BinaryCodedDecimal.FromDecimal((byte)gameDefinition.GameTimeMinutes)));
			packet.Data.Add(new Signature(SignatureType.Data, BinaryCodedDecimal.FromDecimal((byte)gameDefinition.Tags)));
			packet.Data.Add(new Signature(SignatureType.Data, BinaryCodedDecimal.FromDecimal((byte)gameDefinition.Reloads)));
			packet.Data.Add(new Signature(SignatureType.Data, BinaryCodedDecimal.FromDecimal((byte)gameDefinition.Shields)));
			packet.Data.Add(new Signature(SignatureType.Data, BinaryCodedDecimal.FromDecimal((byte)gameDefinition.Mega)));

	        var flags1 = (byte) ((gameDefinition.ExtendedTagging ? 1 : 0) << 7 |
								 (gameDefinition.LimitedReloads ? 1 : 0) << 6 |
								 (gameDefinition.LimitedMega ? 1 : 0) << 5 |
	                             (gameDefinition.TeamTags ? 1 : 0) << 4 |
	                             (gameDefinition.MedicMode ? 1 : 0) << 3 |
	                             (gameDefinition.SlowTags ? 1 : 0) << 2 |
	                             (gameDefinition.GameTypeInfo.HuntThePrey ? 1 : 0) << 1 |
	                             (gameDefinition.HuntDirection ? 1 : 0) << 0);

	        var flags2 = (byte) ((gameDefinition.GameTypeInfo.Zones ? 1 : 0) << 7 |
	                             (gameDefinition.GameTypeInfo.TeamZones ? 1 : 0) << 6 |
	                             (gameDefinition.GameTypeInfo.NeutralizePlayersTaggedInZone ? 1 : 0) << 5 |
	                             (gameDefinition.GameTypeInfo.ZonesRevivePlayers ? 1 : 0) << 4 |
	                             (gameDefinition.GameTypeInfo.HospitalZones ? 1 : 0) << 3 |
	                             (gameDefinition.GameTypeInfo.ZonesTagPlayers ? 1 : 0) << 2 |
	                             (gameDefinition.TeamCount & 0x03));
			
			packet.Data.Add(new Signature(SignatureType.Data, flags1));
			packet.Data.Add(new Signature(SignatureType.Data, flags2));

	        //if (!gameDefinition.Name.IsEmpty()) packet.Data.AddRange(gameDefinition.Name.GetSignatures(4, true));

			packet.PopulateChecksum();

			return packet;
		}

		public static Packet AnnounceSpecialGame(GameDefinition gameDefinition)
		{
			var packet = new Packet();
			packet.Type = gameDefinition.GameTypeInfo.PacketType;
			packet.Data.Add(new Signature(SignatureType.Data, gameDefinition.GameId));
			packet.Data.Add(new Signature(SignatureType.Data, BinaryCodedDecimal.FromDecimal((byte)gameDefinition.GameTimeMinutes)));
			packet.Data.Add(new Signature(SignatureType.Data, BinaryCodedDecimal.FromDecimal((byte)gameDefinition.Tags)));
			packet.Data.Add(new Signature(SignatureType.Data, BinaryCodedDecimal.FromDecimal((byte)gameDefinition.Reloads)));
			packet.Data.Add(new Signature(SignatureType.Data, BinaryCodedDecimal.FromDecimal((byte)gameDefinition.Shields)));
			packet.Data.Add(new Signature(SignatureType.Data, BinaryCodedDecimal.FromDecimal((byte)gameDefinition.Mega)));

			byte flags1, flags2;
			switch (gameDefinition.GameType)
			{
				case GameType.HuntTheTagMaster: // TAGM
					flags1 = 0x70;
					flags2 = 0x02;
					break;
				case GameType.TagMasterHideAndSeek: // TMHS
					flags1 = 0x63;
					flags2 = 0x02;
					break;
				case GameType.Respawn: // RESP
					flags1 = 0xe0;
					flags2 = 0x31;
					break;
				case GameType.RespawnTwoTeams: // 2TRS
					flags1 = 0xf8;
					flags2 = 0x32;
					break;
				case GameType.RespawnThreeTeams: // 3TRS
					flags1 = 0xf8;
					flags2 = 0x33;
					break;
				default:
					flags1 = flags2 = 0;
					break;
			}

			packet.Data.Add(new Signature(SignatureType.Data, flags1));
			packet.Data.Add(new Signature(SignatureType.Data, flags2));

			if (!gameDefinition.Name.IsEmpty()) packet.Data.AddRange(gameDefinition.Name.GetSignatures(4, true));

			packet.PopulateChecksum();

			return packet;
		}

		public static Packet TextMessage(String message)
        {
			var packet = new Packet();
			packet.Type = PacketType.TextMessage;

			if (message.Length > 10) message = message.Substring(0, 10);
			var messageChars = message.ToCharArray();
			foreach (var character in messageChars)
			{
				packet.Data.Add(new Signature(SignatureType.Data, character));
			}

			packet.Data.Add(new Signature(SignatureType.Data, 0)); // null terminator

			packet.PopulateChecksum();

			return packet;
		}

		public static Packet AssignPlayer(UInt16 gameId, UInt16 taggerId, TeamPlayerId teamPlayerId)
		{
			var packet = new Packet();
			packet.Type = PacketType.AssignPlayer;
			packet.Data.Add(new Signature(SignatureType.Data, gameId));
			packet.Data.Add(new Signature(SignatureType.Data, taggerId));
			packet.Data.Add(new Signature(SignatureType.Data, teamPlayerId.Packed23));
			packet.PopulateChecksum();

			return packet;
		}

		public static Packet RequestJoinGame(UInt16 gameId, UInt16 taggerId, int preferredTeamNumber)
		{
			var packet = new Packet();
			packet.Type = PacketType.RequestJoinGame;
			packet.Data.Add(new Signature(SignatureType.Data, gameId));
			packet.Data.Add(new Signature(SignatureType.Data, taggerId));
			packet.Data.Add(new Signature(SignatureType.Data, (UInt16)(preferredTeamNumber & 0x3)));
			packet.PopulateChecksum();

			return packet;
		}

		public static Packet Countdown(UInt16 gameId, byte remainingSeconds, int playerCountTeam1, int playerCountTeam2, int playerCountTeam3)
		{
			var packet = new Packet();
			packet.Type = PacketType.AnnounceCountdown;
			packet.Data.Add(new Signature(SignatureType.Data, gameId));
			packet.Data.Add(new Signature(SignatureType.Data, BinaryCodedDecimal.FromDecimal(remainingSeconds)));
			packet.Data.Add(new Signature(SignatureType.Data, (UInt16)(playerCountTeam1 & 0xff)));
			packet.Data.Add(new Signature(SignatureType.Data, (UInt16)(playerCountTeam2 & 0xff)));
			packet.Data.Add(new Signature(SignatureType.Data, (UInt16)(playerCountTeam3 & 0xff)));
			packet.PopulateChecksum();

			return packet;
		}

	    public static Packet RequestTagReport(UInt16 gameId, TeamPlayerId teamPlayerId)
	    {
		    var packet = new Packet();
		    packet.Type = PacketType.RequestTagReport;
		    packet.Data.Add(new Signature(SignatureType.Data, gameId));
			packet.Data.Add(new Signature(SignatureType.Data, teamPlayerId.Packed44));
		    packet.Data.Add(new Signature(SignatureType.Data, 0x0f)); // TODO: Magic Number
		    packet.PopulateChecksum();

		    return packet;
	    }

	    public static Packet RankReport(UInt16 gameId, int teamNumber, int teamRank, int[] playerRanks)
	    {
		    var packet = new Packet();
		    packet.Type = PacketType.RankReport;
		    packet.Data.Add(new Signature(SignatureType.Data, gameId));

		    var flags = (UInt16) (((teamNumber & 0xf) << 4) | (teamRank & 0xf));
		    packet.Data.Add(new Signature(SignatureType.Data, flags));

		    for (int i = 0; i < 8; i++)
		    {
			    if (playerRanks.Length > i)
			    {
				    packet.Data.Add(new Signature(SignatureType.Data, (UInt16) (playerRanks[i] & 0xff)));
			    }
			    else
			    {
				    packet.Data.Add(new Signature(SignatureType.Data, 0));
			    }
		    }
		    packet.PopulateChecksum();

		    return packet;
	    }

		public static Packet AssignPlayerFailed(UInt16 gameId, UInt16 taggerId)
		{
			var packet = new Packet();
			packet.Type = PacketType.AssignPlayerFailed;
			packet.Data.Add(new Signature(SignatureType.Data, gameId));
			packet.Data.Add(new Signature(SignatureType.Data, taggerId));
			packet.PopulateChecksum();

			return packet;
		}
    }
}

