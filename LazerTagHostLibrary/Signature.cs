using System;
using System.ComponentModel;

namespace LazerTagHostLibrary
{
	[ImmutableObject(true)]
	public class Signature : IEquatable<Signature>
	{
		public Signature(SignatureType type, UInt16 data, byte bitCount = 8)
		{
			Type = type;
			var mask = (UInt16)(Math.Pow(2, bitCount) - 1);
			Data = (UInt16)(data & mask);
			BitCount = bitCount;
		}

		public Signature(SignatureType type, BinaryCodedDecimal data, byte bitCount = 8)
			: this(type, data.BinaryCodedValue, bitCount)
		{

		}

		public Signature(UInt16 data, byte bitCount = 8)
			: this(SignatureType.Data, data, bitCount)
		{

		}

		public Signature(BinaryCodedDecimal data, byte bitCount = 8)
			: this(SignatureType.Data, data, bitCount)
		{

		}

		public SignatureType Type { get; private set; }
		public UInt16 Data { get; private set; }
		public byte BitCount { get; private set; }

		public static bool operator ==(Signature first, Signature second)
		{
			if (ReferenceEquals(null, first) && ReferenceEquals(null, second)) return true; // both null
			if (ReferenceEquals(null, first) ^ ReferenceEquals(null, second)) return false; // one null but not both
			return first.Type == second.Type && first.Data == second.Data && first.BitCount == second.BitCount;
		}

		public static bool operator !=(Signature first, Signature second)
		{
			return !(first == second);
		}

		public bool Equals(Signature other)
		{
			return this == other;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			return obj is Signature && Equals((Signature)obj);
		}

		public override int GetHashCode()
		{
			return (int) Type ^ Data ^ BitCount;
		}

		public override string ToString()
		{
			var typeString = Type == SignatureType.Data ? "" : string.Format("{0}: ", Type);

			if (BitCount < 8)
			{
				return String.Format("{0}0x{1:X2} ({2})", typeString, Data, BitCount);
			}
			else if (BitCount == 8)
			{
				return String.Format("{0}0x{1:X2}", typeString, Data);
			}
			else // if (BitCount > 8)
			{
				return String.Format("{0}0x{1:X3} ({2})", typeString, Data, BitCount);
			}
		}
	}

	public enum SignatureType
	{
		Beacon,
		Tag,
		Packet,
		Data,
		Checksum,
	};
}
