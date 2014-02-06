using System.Diagnostics;

namespace LazerTagHostLibrary
{
	internal class AnnounceGamePacket : Packet
	{
		public AnnounceGamePacket()
		{
			Type = PacketType.AnnounceGameSpecial;
			PopulateData();
			PopulateChecksum();
		}

		public AnnounceGamePacket(Packet packet)
			: base(packet)
		{
			if (Data.Count < 8)
			{
				for (var x = Data.Count; x < 8; x++)
				{
					Data.Add(new Signature(0));
				}
			}
		}

		public byte GameId
		{
			get { return (byte)Data[0].Data; }
			set { Data[0] = new Signature(value); }
		}

		public BinaryCodedDecimal GameLengthMinutes
		{
			get { return new BinaryCodedDecimal(Data[1].Data, true); }
			set { Data[1] = new Signature(value); }
		}

		public BinaryCodedDecimal Tags
		{
			get { return new BinaryCodedDecimal(Data[2].Data, true); }
			set { Data[2] = new Signature(value); }
		}

		public BinaryCodedDecimal Reloads
		{
			get { return new BinaryCodedDecimal(Data[3].Data, true); }
			set { Data[3] = new Signature(value); }
		}

		public BinaryCodedDecimal Shields
		{
			get { return new BinaryCodedDecimal(Data[4].Data, true); }
			set { Data[4] = new Signature(value); }
		}

		public BinaryCodedDecimal Mega
		{
			get { return new BinaryCodedDecimal(Data[5].Data, true); }
			set { Data[5] = new Signature(value); }
		}

		// Flags 1
		public bool NeutralizeAfterTenTags
		{
			get { return Flags1.Get(7); }
			set { Flags1.Set(7, value); }
		}

		public bool LimitedReloads
		{
			get { return Flags1.Get(6); }
			set { Flags1.Set(6, value); }
		}

		public bool LimitedMega
		{
			get { return Flags1.Get(5); }
			set { Flags1.Set(5, value); }
		}

		public bool TeamTags
		{
			get { return Flags1.Get(4); }
			set { Flags1.Set(4, value); }
		}

		public bool MedicMode
		{
			get { return Flags1.Get(3); }
			set { Flags1.Set(3, value); }
		}

		public bool SlowTags
		{
			get { return Flags1.Get(2); }
			set { Flags1.Set(2, value); }
		}

		public bool Hunt
		{
			get { return Flags1.Get(1); }
			set { Flags1.Set(1, value); }
		}

		public bool HuntDirection
		{
			get { return Flags1.Get(0); }
			set { Flags1.Set(0, value); }
		}

		// Flags 2
		public bool ContendedZones
		{
			get { return Flags2.Get(7); }
			set { Flags2.Set(7, value); }
		}

		public bool TeamZones
		{
			get { return Flags2.Get(6); }
			set { Flags2.Set(6, value); }
		}

		public bool NeutralizeAfterOneTag
		{
			get { return Flags2.Get(5); }
			set { Flags2.Set(5, value); }
		}

		public bool ZonesRevive
		{
			get { return Flags2.Get(4); }
			set { Flags2.Set(4, value); }
		}

		public bool HospitalZones
		{
			get { return Flags2.Get(3); }
			set { Flags2.Set(3, value); }
		}

		public bool ZonesTag
		{
			get { return Flags2.Get(2); }
			set { Flags2.Set(2, value); }
		}

		public byte TeamCount
		{
			get { return Flags2.Get(0, 0x3); }
			set { Flags2.Set(0, 0x3, value); }
		}

		public LazerTagString GameName
		{
			get
			{
				if (Data.Count < 12) return new LazerTagString();
				return new LazerTagString(Data.GetRange(8, 4));
			}
			set
			{
				var signatures = value.GetSignatures(4, true);
				if (Data.Count < 8)
				{
					Debug.Assert(false);
				}
				else if (Data.Count < 12)
				{
					Data.AddRange(signatures);
				}
				else
				{
					Data.RemoveRange(8, 4);
					Data.InsertRange(8, value.GetSignatures(4, true));
				}
			}
		}

		private void PopulateData()
		{
			Data.Clear();

			DataAdd(GameId);
			DataAdd(GameLengthMinutes);
			DataAdd(Tags);
			DataAdd(Reloads);
			DataAdd(Shields);
			DataAdd(Mega);
			DataAdd(Flags1);
			DataAdd(Flags2);

			if (Type == PacketType.AnnounceGameSpecial && !GameName.IsEmpty()) DataAdd(GameName, 4, true);
		}

		private FlagsByte Flags1
		{
			get { return new FlagsByte((byte) Data[6].Data); }
			set { Data[6] = new Signature(value); } // TODO: Make sure the signature gets updated when the value changes
		}

		private FlagsByte Flags2
		{
			get { return new FlagsByte((byte)Data[7].Data); }
			set { Data[7] = new Signature(value); }
		}
	}
}
