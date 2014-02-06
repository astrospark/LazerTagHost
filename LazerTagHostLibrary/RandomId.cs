using System;
using System.ComponentModel;

namespace LazerTagHostLibrary
{
	[ImmutableObject(true)]
	public class RandomId
	{
		public RandomId()
		{
			Id = Generate();
		}

		public RandomId(byte id)
		{
			Id = id;
		}

		public byte Id { get; private set; }

		// RandomId -> byte
		public static implicit operator byte(RandomId input)
		{
			return input.Id;
		}

		// byte -> RandomId
		public static implicit operator RandomId(byte input)
		{
			return new RandomId(input);
		}

		protected static byte Generate()
		{
			return (byte)(new Random().Next() & 0xff);
		}
	}
}
