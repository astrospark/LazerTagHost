using System;
using System.Collections.Generic;

namespace LazerTagHostLibrary
{
	public class Packet
	{
		public Packet()
		{
			
		}

		public Packet(PacketType type)
		{
			Type = type;
		}

		public List<Signature> Signatures
		{
			get
			{
				var signatures = new List<Signature>();
				if (PacketTypeSignature != null) signatures.Add(PacketTypeSignature);
				signatures.AddRange(Data);
				if (Checksum != null) signatures.Add(Checksum);
				return signatures;
			}
		}

		public Signature PacketTypeSignature { get; set; }

		public bool PacketTypeSignatureValid
		{
			get
			{
				if (PacketTypeSignature == null) return false;
				if (PacketTypeSignature.Type != SignatureType.Packet) return false;
				if (PacketTypeSignature.BitCount != 8) return false;
				return true;
			}
		}

		public PacketType Type
		{
			get
			{
				if (!PacketTypeSignatureValid) throw new InvalidOperationException();
				return (PacketType) PacketTypeSignature.Data;
			}
			set
			{
				PacketTypeSignature = new Signature(SignatureType.Packet, (UInt16) value);
			}
		}

		private readonly List<Signature> _data = new List<Signature>();
		public List<Signature> Data
		{
			get { return _data; }
		}

		public bool DataValid
		{
			get
			{
				if (Data == null) return false;
				if (Data.Count < 1) return false;
				foreach (var signature in Data)
				{
					if (signature.Type != SignatureType.Data) return false;
					if (PacketTypeSignature.BitCount < 1) return false;
					if (PacketTypeSignature.BitCount == 9) return false;
				}
				return true;
			}
		}

		public Signature Checksum { get; set; }

		public bool ChecksumValid
		{
			get { return Checksum == CalculateChecksum(this); }
		}

		public Signature PopulateChecksum()
		{
			Checksum = CalculateChecksum(this);
			return Checksum;
		}

		private static Signature CalculateChecksum(Packet packet)
		{
			var checksum = (byte) packet.PacketTypeSignature.Data;

			foreach (var signature in packet.Data)
			{
				if (signature.Type != SignatureType.Data) continue;
				checksum += (byte) signature.Data;
			}

			return new Signature(SignatureType.Checksum, checksum);
		}

		public bool Valid
		{
			get { return PacketTypeSignatureValid && DataValid && ChecksumValid; }
		}

		public override string ToString()
		{
			var values = new List<string>();
			foreach (var signature in Signatures)
			{
				if (signature != null) values.Add(signature.ToString());
			}
			return String.Join(", ", values);
		}
	}

	public enum PacketType
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
		AssignPlayerFailed = 0x0f,

		AnnounceCountdown = 0x00,

		RequestTagReport = 0x31,

		TagSummary = 0x40,
		TeamOneTagReport = 0x41,
		TeamTwoTagReport = 0x42,
		TeamThreeTagReport = 0x43,

		RankReport = 0x32,

		SingleTagReport = 0x48,
		TextMessage = 0x80,
		SpecialAttack = 0x90,
	};
}
