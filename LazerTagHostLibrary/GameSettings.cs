using System.ComponentModel;

namespace LazerTagHostLibrary
{
	public class GameSettings : IGameSettingsReadOnly
	{
		public BinaryCodedDecimal TagsAmount { get; set; }
		public BinaryCodedDecimal ReloadsAmount { get; set; }
		public BinaryCodedDecimal ShieldsAmount { get; set; }
		public BinaryCodedDecimal MegaAmount { get; set; }
		public bool NeutralizeAfterOneTag { get; set; }
		public bool NeutralizeAfterTenTags { get; set; }
		public bool HasTeamTags { get; set; }
		public bool HasMedicMode { get; set; }
		public bool HasSlowTags { get; set; }
		public bool IsHuntThePrey { get; set; }
		public bool TeamTwoHuntsFirst { get; set; }
		public bool HasContestedZones { get; set; }
		public bool ZonesCanHaveTeams { get; set; }
		public bool HasUnneutralizeZones { get; set; }
		public bool HasHealingZones { get; set; }
		public bool HasHostileZones { get; set; }
	}

	[ImmutableObject(true)]
	public interface IGameSettingsReadOnly
	{
		BinaryCodedDecimal TagsAmount { get; }
		BinaryCodedDecimal ReloadsAmount { get; }
		BinaryCodedDecimal ShieldsAmount { get; }
		BinaryCodedDecimal MegaAmount { get; }
		bool NeutralizeAfterOneTag { get; }
		bool NeutralizeAfterTenTags { get; }
		bool HasTeamTags { get; }
		bool HasMedicMode { get; }
		bool HasSlowTags { get; }
		bool IsHuntThePrey { get; }
		bool TeamTwoHuntsFirst { get; }
		bool HasContestedZones { get; }
		bool ZonesCanHaveTeams { get; }
		bool HasUnneutralizeZones { get; }
		bool HasHealingZones { get; }
		bool HasHostileZones { get; }
	}
}
