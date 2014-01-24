namespace LazerTagHostLibrary
{
	public class BinaryCodedDecimal
	{
		public BinaryCodedDecimal()
		{

		}

		public BinaryCodedDecimal(int decimalValue)
		{
			BinaryCodedValue = FromDecimal(decimalValue);
		}

		public static byte ToDecimal(int bcd)
		{
			if (bcd < 0 || bcd >= 0xff) return 0xff;
			return (byte) ((((bcd >> 4) & 0xf)*10) + (bcd & 0xf));
		}

		public static byte FromDecimal(int dec)
		{
			if (dec < 0 || dec >= 0xff) return 0xff;
			return (byte) (((dec/10) << 4) | (dec%10));
		}

		public byte BinaryCodedValue { get; set; }

		public byte DecimalValue
		{
			get { return ToDecimal(BinaryCodedValue); }
			set { BinaryCodedValue = FromDecimal(value); }
		}

		// BinaryCodedDecimal -> byte
		public static implicit operator byte(BinaryCodedDecimal input)
		{
			return input.DecimalValue;
		}

		// int -> BinaryCodedDecimal
		public static explicit operator BinaryCodedDecimal(int input)
		{
			return new BinaryCodedDecimal(input);
		}
	}
}
