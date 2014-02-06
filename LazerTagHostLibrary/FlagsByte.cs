namespace LazerTagHostLibrary
{
	class FlagsByte
	{
		public FlagsByte(byte value = 0)
		{
			_value = value;
		}

		public bool Get(byte bit)
		{
			return Get(bit, 1) == 1;
		}

		public byte Get(byte bit, byte mask)
		{
			return (byte) ((_value >> bit) & mask);
		}

		public void Set(byte bit, bool value)
		{
			if (value) Set(bit); else Clear(bit);
		}

		public void Set(byte bit, byte mask, byte value)
		{
			_value &= (byte) ~(mask << bit);
			_value |= (byte) ((value & mask) << bit);
		}

		public void Set(byte bit)
		{
			_value |= (byte) (1 << bit);
		}

		public void Clear(byte bit)
		{
			_value &= (byte) ~(1 << bit);
		}

		public static implicit operator byte(FlagsByte input)
		{
			return input._value;
		}

		public static implicit operator FlagsByte(byte input)
		{
			return new FlagsByte(input);
		}

		private byte _value;
	}
}
