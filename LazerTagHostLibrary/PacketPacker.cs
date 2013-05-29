using System;

namespace LazerTagHostLibrary
{
    public class PacketPacker
    {
		public static IrPacket[] Shot(TeamPlayerId teamPlayerId, int damage)
		{
			var flags =
				(byte) ((teamPlayerId.Packed23 << 2) |
				        (damage & 0x3));
			
			var values = new []
				{
					new IrPacket(IrPacket.PacketTypes.Data, flags, 7) 
				};
			
			return values;
		}

		public static IrPacket[] Shot(int damage)
        {
	        return Shot(new TeamPlayerId(0), damage);
        }

        public static IrPacket[] Beacon(int teamNumber, bool tagReceived, int tagStrength)
        {
	        if (!tagReceived) tagStrength = 0;
	        
			var flags =
		        (byte) (((teamNumber & 0x3) << 3) |
		                ((tagReceived ? 1 : 0) << 2) |
		                ((tagStrength & 0x3)));
	        
			var values = new []
		        {
			        new IrPacket(IrPacket.PacketTypes.Beacon, flags, 5) 
		        };

			return values;
        }

		public static IrPacket[] ZoneBeacon(int teamNumber, HostGun.ZoneType zoneType)
		{
			var flags =
				(byte) (((teamNumber & 0x3) << 3) |
				        ((byte) zoneType & 0x3));

			var values = new[]
		        {
			        new IrPacket(IrPacket.PacketTypes.Beacon, flags, 5) 
		        };
			
			return values;
		}

        public static UInt16[] AnnounceGame(GameDefinition gameDefinition)
        {
	        if (gameDefinition.GameTypeInfo.CommandCode == HostGun.CommandCode.AnnounceGameSpecial)
		        return AnnounceSpecialGame(gameDefinition);

	        var flags1 =
		        (byte) ((gameDefinition.ExtendedTagging ? 1 : 0) << 7 |
		                (gameDefinition.UnlimitedMega ? 1 : 0) << 6 |
		                (gameDefinition.UnlimitedReloads ? 1 : 0) << 5 |
		                (gameDefinition.TeamTags ? 1 : 0) << 4 |
		                (gameDefinition.MedicMode ? 1 : 0) << 3 |
		                (gameDefinition.RapidTags ? 1 : 0) << 2 |
		                (gameDefinition.GameTypeInfo.HuntThePrey ? 1 : 0) << 1 |
		                (gameDefinition.HuntDirection ? 1 : 0) << 0);
	        var flags2 =
		        (byte) ((gameDefinition.GameTypeInfo.Zones ? 1 : 0) << 7 |
		                (gameDefinition.GameTypeInfo.TeamZones ? 1 : 0) << 6 |
		                (gameDefinition.GameTypeInfo.NeutralizePlayersTaggedInZone ? 1 : 0) << 5 |
		                (gameDefinition.GameTypeInfo.ZonesRevivePlayers ? 1 : 0) << 4 |
		                (gameDefinition.GameTypeInfo.HospitalZones ? 1 : 0) << 3 |
		                (gameDefinition.GameTypeInfo.ZonesTagPlayers ? 1 : 0) << 2 |
		                (gameDefinition.TeamCount & 0x03));

	        var values = new[]
		        {
			        (UInt16) gameDefinition.GameTypeInfo.CommandCode,
			        gameDefinition.GameId,
			        HexCodedDecimal.FromDecimal((byte) gameDefinition.GameTimeMinutes),
			        HexCodedDecimal.FromDecimal((byte) gameDefinition.Tags),
			        HexCodedDecimal.FromDecimal((byte) gameDefinition.Reloads),
			        HexCodedDecimal.FromDecimal((byte) gameDefinition.Shields),
			        HexCodedDecimal.FromDecimal((byte) gameDefinition.Mega),
			        flags1,
			        flags2
		        };

			if (gameDefinition.Name != null)
			{
                var newValues = new UInt16[values.Length + 4];
				values.CopyTo(newValues, 0);
				gameDefinition.Name.CopyTo(newValues, values.Length);
				values = newValues;
			}

            return values;
        }

		public static UInt16[] AnnounceSpecialGame(GameDefinition gameDefinition)
		{
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
			var values = new[]
		        {
			        (UInt16) gameDefinition.GameTypeInfo.CommandCode,
			        gameDefinition.GameId,
			        HexCodedDecimal.FromDecimal((byte) gameDefinition.GameTimeMinutes),
			        HexCodedDecimal.FromDecimal((byte) gameDefinition.Tags),
			        HexCodedDecimal.FromDecimal((byte) gameDefinition.Reloads),
			        HexCodedDecimal.FromDecimal((byte) gameDefinition.Shields),
			        HexCodedDecimal.FromDecimal((byte) gameDefinition.Mega),
			        flags1,
			        flags2
		        };

			if (gameDefinition.Name != null)
			{
				var newValues = new UInt16[values.Length + 4];
				values.CopyTo(newValues, 0);
				gameDefinition.Name.CopyTo(newValues, values.Length);
				values = newValues;
			}

			return values;
		}

		public static UInt16[] TextMessage(String message)
        {
			if (message.Length > 10) message = message.Substring(0, 10);

			var values = new UInt16[message.Length + 2];

	        values[0] = (UInt16) HostGun.CommandCode.TextMessage;

			var messageChars = message.ToCharArray();
			for (var i = 0; i < messageChars.Length; i++)
			{
				values[i + 1] = messageChars[i];
			}

			values[message.Length + 1] = 0;

			return values;
        }
    }
}

