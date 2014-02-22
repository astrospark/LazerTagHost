using System;
using System.ComponentModel;

namespace LazerTagHostLibrary
{
	[ImmutableObject(true)]
	public struct BinaryCodedDecimal : IConvertible
	{
		public BinaryCodedDecimal(int value, bool binaryCoded = false) : this()
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

		public TypeCode GetTypeCode()
		{
			return TypeCode.Byte;
		}

		public bool ToBoolean(IFormatProvider provider)
		{
			return Convert.ToBoolean(DecimalValue, provider);
		}

		public char ToChar(IFormatProvider provider)
		{
			return Convert.ToChar(DecimalValue, provider);
		}

		public sbyte ToSByte(IFormatProvider provider)
		{
			return Convert.ToSByte(DecimalValue, provider);
		}

		public byte ToByte(IFormatProvider provider)
		{
			return Convert.ToByte(DecimalValue, provider);
		}

		public short ToInt16(IFormatProvider provider)
		{
			return Convert.ToInt16(DecimalValue, provider);
		}

		public ushort ToUInt16(IFormatProvider provider)
		{
			return Convert.ToUInt16(DecimalValue, provider);
		}

		public int ToInt32(IFormatProvider provider)
		{
			return Convert.ToInt32(DecimalValue, provider);
		}

		public uint ToUInt32(IFormatProvider provider)
		{
			return Convert.ToUInt32(DecimalValue, provider);
		}

		public long ToInt64(IFormatProvider provider)
		{
			return Convert.ToInt64(DecimalValue, provider);
		}

		public ulong ToUInt64(IFormatProvider provider)
		{
			return Convert.ToUInt64(DecimalValue, provider);
		}

		public float ToSingle(IFormatProvider provider)
		{
			return Convert.ToSingle(DecimalValue, provider);
		}

		public double ToDouble(IFormatProvider provider)
		{
			return Convert.ToDouble(DecimalValue, provider);
		}

		public decimal ToDecimal(IFormatProvider provider)
		{
			return Convert.ToDecimal(DecimalValue, provider);
		}

		public DateTime ToDateTime(IFormatProvider provider)
		{
			return Convert.ToDateTime(DecimalValue, provider);
		}

		public string ToString(IFormatProvider provider)
		{
			return Convert.ToString(DecimalValue, provider);
		}

		public object ToType(Type conversionType, IFormatProvider provider)
		{
			return Convert.ChangeType(DecimalValue, conversionType, provider);
		}
	}
}
