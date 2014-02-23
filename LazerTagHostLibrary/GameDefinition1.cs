using System.ComponentModel;

namespace LazerTagHostLibrary
{
	[ImmutableObject(true)]
	public class GameDefinition1
	{
		public GameType1 GameType { get; private set; }
		public Team1[] Teams { get; private set; }
		public Player1[] Players { get; private set; }
		public BinaryCodedDecimal GameLengthMinutes { get; private set; }
	}
}
