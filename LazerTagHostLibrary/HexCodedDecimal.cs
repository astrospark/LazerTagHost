namespace LazerTagHostLibrary
{
	public static class HexCodedDecimal
	{
		public static int ToDecimal(int hexCodedDecimal)
		{
			if (hexCodedDecimal == 0xff) return hexCodedDecimal;
			return (((hexCodedDecimal >> 4) & 0xf) * 10) + (hexCodedDecimal & 0xf);
		}

		public static byte FromDecimal(byte dec)
		{
			if (dec == 0xff) return dec;
			return (byte)(((dec / 10) << 4) | (dec % 10));
		}
	}
}
