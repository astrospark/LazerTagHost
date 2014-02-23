using System.ComponentModel;
namespace LazerTagHostLibrary
{
	[ImmutableObject(true)]
	public class GameTypeTeam
	{
		public GameTypeTeam(
			int number = 0,
			string name = "",
			string description = "",
			GameSettings gameSettings = null)
		{
			Number = number;
			Name = name;
			Description = description;
			GameSettings = gameSettings;
		}

		public int Number { get; private set; }
		public string Name { get; private set; }
		public string Description { get; private set; }
		public GameSettings GameSettings { get; private set; }
	}
}
