using System;

namespace LazerTagHostLibrary
{
    public class IrPacket
	{
		public IrPacket(PacketTypes packetType, UInt16 data, UInt16 bitCount)
		{
			Type = packetType;
			Data = data;
			BitCount = bitCount;
		}

		public PacketTypes Type { get; set; }
		public UInt16 Data { get; set; }
		public UInt16 BitCount { get; set; }

		public enum PacketTypes
		{
            Data,
            Beacon,
        };
    }
}
