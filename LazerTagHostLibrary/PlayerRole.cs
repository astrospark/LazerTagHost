using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace LazerTagHostLibrary
{
	[ImmutableObject(true)]
	public class PlayerRole : IImmutableObject
	{
		public string Name { get; private set; }
		public LazerTagString ShortName { get; private set; }
		public string Description { get; private set; }
		public bool IsOptional { get; private set; }
		public ReadOnlyCollection<int> AllowedTeams { get; private set; }
		public ReadOnlyCollection<int> AllowedSquads { get; private set; }
		public Range<int> PlayerRange { get; private set; }
		public GameSettings GameSettings { get; private set; }

		public sealed class Builder : ImmutableObjectBuilder<PlayerRole>
		{
			public string Name
			{
				get { return _instance.Name; }
				set { _instance.Name = value; }
			}

			public LazerTagString ShortName
			{
				get { return _instance.ShortName; }
				set { _instance.ShortName = value; }
			}

			public string Description
			{
				get { return _instance.Description; }
				set { _instance.Description = value; }
			}

			public bool IsOptional
			{
				get { return _instance.IsOptional; }
				set { _instance.IsOptional = value; }
			}

			public IList<int> AllowedTeams
			{
				get { return _instance.AllowedTeams; }
				set { _instance.AllowedTeams = new ReadOnlyCollection<int>(value); }
			}

			public IList<int> AllowedSquads
			{
				get { return _instance.AllowedSquads; }
				set { _instance.AllowedSquads = new ReadOnlyCollection<int>(value); }
			}

			public Range<int> PlayerRange
			{
				get { return _instance.PlayerRange; }
				set { _instance.PlayerRange = value; }
			}

			public GameSettings GameSettings
			{
				get { return _instance.GameSettings; }
				set { _instance.GameSettings = value; }
			}

			protected override PlayerRole Build()
			{
				return _instance;
			}

			private readonly PlayerRole _instance = new PlayerRole();
		}
	}
}
