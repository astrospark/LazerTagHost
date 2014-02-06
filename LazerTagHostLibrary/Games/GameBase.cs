using System;

namespace LazerTagHostLibrary.Games
{
	public class GameBase
	{
		public virtual Packet GetAnnouncementPacket()
		{
			var gameTypeInfo = GameTypes.GetInfo(GameType);

			var packet = new Packet(gameTypeInfo.PacketType);
			packet.Data.Add(new Signature(SignatureType.Data, GameId));
			packet.Data.Add(new Signature(SignatureType.Data, GameLengthMinutes));
			packet.Data.Add(new Signature(SignatureType.Data, Tags));
			packet.Data.Add(new Signature(SignatureType.Data, Reloads));
			packet.Data.Add(new Signature(SignatureType.Data, Shields));
			packet.Data.Add(new Signature(SignatureType.Data, Mega));

			packet.Data.Add(new Signature(SignatureType.Data, Flags1));
			packet.Data.Add(new Signature(SignatureType.Data, Flags2));

			if (!GameName.IsEmpty()) packet.Data.AddRange(GameName.GetSignatures(4, true));

			packet.PopulateChecksum();

			return packet;
		}

		public virtual void CalculateScores()
		{

		}

		public GameType GameType { get; set; }

		public byte GameId { get; set; }
		public BinaryCodedDecimal GameLengthMinutes { get; set; }
		public BinaryCodedDecimal Tags { get; set; }
		public BinaryCodedDecimal Reloads { get; set; }
		public BinaryCodedDecimal Shields { get; set; }
		public BinaryCodedDecimal Mega { get; set; }

		public bool TeamTags { get; set; }
		public bool MedicMode { get; set; }
		public bool SlowTags { get; set; }

		public bool IsZoneGame { get; set; }

		public int TeamCount { get; set; }

		public byte Flags1 { get; set; }
		public byte Flags2 { get; set; }

		public bool IsTeamGame
		{
			get { return (TeamCount > 1); }
		}

		public LazerTagString GameName { get; set; }
	}
}
