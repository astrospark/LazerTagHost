using System.ComponentModel;

namespace LazerTagHostLibrary
{
	[ImmutableObject(true)]
	public class GameTypeZone
	{
		public GameTypeZone(
			string name = "",
			string description = "",
			GameZoneType zoneType = 0,
			int teamNumber = 0,
			int beaconFrequencyMilliseconds = 0,
			bool isHostile = false,
			int hostileTagFrequencyMilliseconds = 0,
			bool isMedic = false,
			int medicTagsPerRequest = 0,
			int medicPlayerWaitSeconds = 0,
			int medicTeamWaitSeconds = 0,
			int medicMaxTagsPerPlayer = 0,
			int medicMaxTagsPerTeam = 0)
		{
			Name = name;
			Description = description;
			ZoneType = zoneType;
			TeamNumber = teamNumber;
			BeaconFrequencyMilliseconds = beaconFrequencyMilliseconds;
			IsHostile = isHostile;
			HostileTagFrequencyMilliseconds = hostileTagFrequencyMilliseconds;
			IsMedic = isMedic;
			MedicTagsPerRequest = medicTagsPerRequest;
			MedicPlayerWaitSeconds = medicPlayerWaitSeconds;
			MedicTeamWaitSeconds = medicTeamWaitSeconds;
			MedicMaxTagsPerPlayer = medicMaxTagsPerPlayer;
			MedicMaxTagsPerTeam = medicMaxTagsPerTeam;
		}

		// TODO: allow user to change settings
		public string Name { get; private set; }
		public string Description { get; private set; }
		public GameZoneType ZoneType { get; private set; }
		public int TeamNumber { get; private set; }
		public int BeaconFrequencyMilliseconds { get; private set; }
		public bool IsHostile { get; private set; }
		public int HostileTagFrequencyMilliseconds { get; private set; }
		public bool IsMedic { get; private set; }
		public int MedicTagsPerRequest { get; private set; }
		public int MedicPlayerWaitSeconds { get; private set; }
		public int MedicTeamWaitSeconds { get; private set; }
		public int MedicMaxTagsPerPlayer { get; private set; }
		public int MedicMaxTagsPerTeam { get; private set; }
	}

	public enum GameZoneType
	{
		Uncontested,
		Contested,
	}
}
