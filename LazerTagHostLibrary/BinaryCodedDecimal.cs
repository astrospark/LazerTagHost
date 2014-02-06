using System.ComponentModel;

namespace LazerTagHostLibrary
{
	[ImmutableObject(true)]
	public class BinaryCodedDecimal
	{
		public BinaryCodedDecimal(int value, bool binaryCoded = false)
		{
			DecimalValue = binaryCoded ? ToDecimal(value) : (byte) value;
		}

		public byte DecimalValue { get; private set; }

		public byte BinaryCodedValue
		{
			get { return FromDecimal(DecimalValue); }
		}

		// BinaryCodedDecimal -> byte (decimal)
		public static implicit operator byte(BinaryCodedDecimal input)
		{
			return input.DecimalValue;
		}

		// int (decimal) -> BinaryCodedDecimal
		public static explicit operator BinaryCodedDecimal(int input)
		{
			return new BinaryCodedDecimal(input);
		}

		public static byte ToDecimal(int bcd)
		{
			if (bcd < 0 || bcd >= 0xff) return 0xff;
			return (byte)((((bcd >> 4) & 0xf) * 10) + (bcd & 0xf));
		}

		public static byte FromDecimal(int dec)
		{
			if (dec < 0 || dec >= 0xff) return 0xff;
			return (byte)(((dec / 10) << 4) | (dec % 10));
		}
	}
}
