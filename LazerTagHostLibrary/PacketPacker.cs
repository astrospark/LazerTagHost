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

        public static UInt16[] Beacon(TeamPlayerId teamPlayerId, bool tagReceived, int tagStrength)
        {
	        if (!tagReceived) tagStrength = 0;
	        
			var flags =
		        (byte) (((teamPlayerId.TeamNumber & 0x3) << 3) |
		                ((tagReceived ? 1 : 0) << 2) |
		                ((tagStrength & 0x3)));
	        
			var values = new UInt16[]
		        {
			        flags
		        };
            
			return values;
        }

		public static UInt16[] ZoneBeacon(TeamPlayerId teamPlayerId, HostGun.ZoneType zoneType)
		{
			var flags =
				(byte) (((teamPlayerId.TeamNumber & 0x3) << 3) |
				        ((byte) zoneType & 0x3));
			
			var values = new UInt16[]
			{
                flags
            };
			return values;
		}

        public static UInt16[] GameDefinition(GameDefinition gameDefinition)
        {
	        var flags1 =
		        (byte) ((gameDefinition.ExtendedTagging ? 1 : 0) << 7 |
		                (gameDefinition.UnlimitedMega ? 1 : 0) << 6 |
		                (gameDefinition.UnlimitedReloads ? 1 : 0) << 5 |
		                (gameDefinition.TeamTags ? 1 : 0) << 4 |
		                (gameDefinition.MedicMode ? 1 : 0) << 3 |
		                (gameDefinition.RapidTags ? 1 : 0) << 2 |
		                (gameDefinition.Hunt ? 1 : 0) << 1 |
		                (gameDefinition.HuntDirection ? 1 : 0) << 0);
	        var flags2 =
		        (byte) ((gameDefinition.Zones ? 1 : 0) << 7 |
		                (gameDefinition.TeamZones ? 1 : 0) << 6 |
		                (gameDefinition.NeutralizeTaggedPlayers ? 1 : 0) << 5 |
		                (gameDefinition.ZonesRevivePlayers ? 1 : 0) << 4 |
		                (gameDefinition.HospitalZones ? 1 : 0) << 3 |
		                (gameDefinition.ZonesTagPlayers ? 1 : 0) << 2 |
		                (gameDefinition.TeamCount & 0x03));
	        
			var values = new[]
		        {
			        (UInt16) gameDefinition.GameType,
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
				gameDefinition.Name.CopyTo(newValues, values.Length);
				values = newValues;
			}

            return values;
        }

        public static UInt16[] TextMessage(String message)
        {
			if (message.Length > 10) message = message.Substring(0, 10);

			var values = new UInt16[message.Length + 2];

	        values[0] = (UInt16) HostGun.CommandCode.COMMAND_CODE_TEXT_MESSAGE;

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

