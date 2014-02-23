using System.ComponentModel;

namespace LazerTagHostLibrary
{
	[ImmutableObject(true)]
	public class GameSettings
	{
		public GameSettings(
			GameSettings gameSettings = null,
			GameSetting<BinaryCodedDecimal> tagsAmount = new GameSetting<BinaryCodedDecimal>(),
			GameSetting<BinaryCodedDecimal> reloadsAmount = new GameSetting<BinaryCodedDecimal>(),
			GameSetting<BinaryCodedDecimal> shieldsAmount = new GameSetting<BinaryCodedDecimal>(),
			GameSetting<BinaryCodedDecimal> megaAmount = new GameSetting<BinaryCodedDecimal>(),
			GameSetting<bool> neutralizeAfterOneTag = new GameSetting<bool>(),
			GameSetting<bool> neutralizeAfterTenTags = new GameSetting<bool>(),
			GameSetting<bool> hasTeamTags = new GameSetting<bool>(),
			GameSetting<bool> hasMedicMode = new GameSetting<bool>(),
			GameSetting<bool> hasSlowTags = new GameSetting<bool>(),
			GameSetting<bool> isHuntThePrey = new GameSetting<bool>(),
			GameSetting<bool> reverseHuntDirection = new GameSetting<bool>(),
			GameSetting<bool> hasContestedZones = new GameSetting<bool>(),
			GameSetting<bool> zonesCanHaveTeams = new GameSetting<bool>(),
			GameSetting<bool> hasUnneutralizeZones = new GameSetting<bool>(),
			GameSetting<bool> hasHealingZones = new GameSetting<bool>(),
			GameSetting<bool> hasHostileZones = new GameSetting<bool>())
		{
			TagsAmount = (gameSettings == null || tagsAmount.IsSet) ? tagsAmount : gameSettings.TagsAmount;
			ReloadsAmount = (gameSettings == null || reloadsAmount.IsSet) ? reloadsAmount : gameSettings.ReloadsAmount;
			ShieldsAmount = (gameSettings == null || shieldsAmount.IsSet) ? shieldsAmount : gameSettings.ShieldsAmount;
			MegaAmount = (gameSettings == null || megaAmount.IsSet) ? megaAmount : gameSettings.MegaAmount;
			NeutralizeAfterOneTag = (gameSettings == null || neutralizeAfterOneTag.IsSet) ? neutralizeAfterOneTag : gameSettings.NeutralizeAfterOneTag;
			NeutralizeAfterTenTags = (gameSettings == null || neutralizeAfterTenTags.IsSet) ? neutralizeAfterTenTags : gameSettings.NeutralizeAfterTenTags;
			HasTeamTags = (gameSettings == null || hasTeamTags.IsSet) ? hasTeamTags : gameSettings.HasTeamTags;
			HasMedicMode = (gameSettings == null || hasMedicMode.IsSet) ? hasMedicMode : gameSettings.HasMedicMode;
			HasSlowTags = (gameSettings == null || hasSlowTags.IsSet) ? hasSlowTags : gameSettings.HasSlowTags;
			IsHuntThePrey = (gameSettings == null || isHuntThePrey.IsSet) ? isHuntThePrey : gameSettings.IsHuntThePrey;
			ReverseHuntDirection = (gameSettings == null || reverseHuntDirection.IsSet) ? reverseHuntDirection : gameSettings.ReverseHuntDirection;
			HasContestedZones = (gameSettings == null || hasContestedZones.IsSet) ? hasContestedZones : gameSettings.HasContestedZones;
			ZonesCanHaveTeams = (gameSettings == null || zonesCanHaveTeams.IsSet) ? zonesCanHaveTeams : gameSettings.ZonesCanHaveTeams;
			HasUnneutralizeZones = (gameSettings == null || hasUnneutralizeZones.IsSet) ? hasUnneutralizeZones : gameSettings.HasUnneutralizeZones;
			HasHealingZones = (gameSettings == null || hasHealingZones.IsSet) ? hasHealingZones : gameSettings.HasHealingZones;
			HasHostileZones = (gameSettings == null || hasHostileZones.IsSet) ? hasHostileZones : gameSettings.HasHostileZones;
		}

		public GameSetting<BinaryCodedDecimal> TagsAmount { get; private set; }
		public GameSetting<BinaryCodedDecimal> ReloadsAmount { get; private set; }
		public GameSetting<BinaryCodedDecimal> ShieldsAmount { get; private set; }
		public GameSetting<BinaryCodedDecimal> MegaAmount { get; private set; }
		public GameSetting<bool> NeutralizeAfterOneTag { get; private set; }
		public GameSetting<bool> NeutralizeAfterTenTags { get; private set; }
		public GameSetting<bool> HasTeamTags { get; private set; }
		public GameSetting<bool> HasMedicMode { get; private set; }
		public GameSetting<bool> HasSlowTags { get; private set; }
		public GameSetting<bool> IsHuntThePrey { get; private set; }
		public GameSetting<bool> ReverseHuntDirection { get; private set; }
		public GameSetting<bool> HasContestedZones { get; private set; }
		public GameSetting<bool> ZonesCanHaveTeams { get; private set; }
		public GameSetting<bool> HasUnneutralizeZones { get; private set; }
		public GameSetting<bool> HasHealingZones { get; private set; }
		public GameSetting<bool> HasHostileZones { get; private set; }
	}
}
