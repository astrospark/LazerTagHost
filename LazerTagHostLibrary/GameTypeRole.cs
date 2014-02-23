using System.Collections.ObjectModel;
using System.ComponentModel;

namespace LazerTagHostLibrary
{
	[ImmutableObject(true)]
	public class GameTypeRole
	{
		public GameTypeRole(
			string name = "",
			LazerTagString shortName = null,
			string description = "",
			ReadOnlyCollection<int> allowedTeams = null,
			Range<int> playerRange = null,
			GameSettings gameSettings = null)
		{
			Name = name;
			ShortName = shortName;
			Description = description;
			AllowedTeams = allowedTeams;
			PlayerRange = playerRange;
			GameSettings = gameSettings;
		}

		public string Name { get; private set; }
		public LazerTagString ShortName { get; private set; }
		public string Description { get; private set; }

		public ReadOnlyCollection<int> AllowedTeams { get; private set; }
		public Range<int> PlayerRange { get; private set; }
		public GameSettings GameSettings { get; private set; }
	}
}
