using System.ComponentModel;

namespace LazerTagHostLibrary
{
	[ImmutableObject(true)]
	public class GameZone
	{
		public string Name { get; private set; }
		public string Description { get; private set; }
		public GameZoneType ZoneType { get; private set; }
		public bool IsHostile { get; private set; }
		public int TeamNumber { get; private set; }
		public int BeaconFrequencyMilliseconds { get; private set; }
		public int HostileTagFrequencyMilliseconds { get; private set; }

		public sealed class Builder : ImmutableObjectBuilder<GameZone>
		{
			public string Name
			{
				get { return _instance.Name; }
				set { _instance.Name = value; }
			}

			public string Description
			{
				get { return _instance.Description; }
				set { _instance.Description = value; }
			}

			public GameZoneType ZoneType
			{
				get { return _instance.ZoneType; }
				set { _instance.ZoneType = value; }
			}

			public bool IsHostile
			{
				get { return _instance.IsHostile; }
				set { _instance.IsHostile = value; }
			}

			public int TeamNumber
			{
				get { return _instance.TeamNumber; }
				set { _instance.TeamNumber = value; }
			}

			public int BeaconFrequencyMilliseconds
			{
				get { return _instance.BeaconFrequencyMilliseconds; }
				set { _instance.BeaconFrequencyMilliseconds = value; }
			}

			public int HostileTagFrequencyMilliseconds
			{
				get { return _instance.HostileTagFrequencyMilliseconds; }
				set { _instance.HostileTagFrequencyMilliseconds = value; }
			}

			protected override GameZone Build()
			{
				return _instance;
			}

			private readonly GameZone _instance = new GameZone();
		}
	}

	public enum GameZoneType
	{
		Uncontested,
		Contested,
	}
}
