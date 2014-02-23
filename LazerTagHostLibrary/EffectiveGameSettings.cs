using System;
using System.ComponentModel;

namespace LazerTagHostLibrary
{
	[ImmutableObject(true)]
	public class EffectiveGameSettings
	{
		public EffectiveGameSettings(EffectiveGameSettings parentEffectiveSettings, GameSettings defaultSettings, GameSettings userSettings)
		{
			Parent = parentEffectiveSettings;
			Default = defaultSettings;
			User = userSettings;
			Effective = GetEffectiveSettings(Parent, Default, User);
		}

		public static GameSettings GetEffectiveSettings(GameSettings parent, GameSettings child)
		{
			if (parent == null && child == null) throw new InvalidOperationException("Both parent and child cannot be null.");
			if (parent == null) parent = new GameSettings();
			if (child == null) child = new GameSettings();

			return new GameSettings(
				tagsAmount: GetEffectiveSetting(parent.TagsAmount, child.TagsAmount),
				reloadsAmount: GetEffectiveSetting(parent.ReloadsAmount, child.ReloadsAmount),
				shieldsAmount: GetEffectiveSetting(parent.ShieldsAmount, child.ShieldsAmount),
				megaAmount: GetEffectiveSetting(parent.MegaAmount, child.MegaAmount),
				neutralizeAfterOneTag: GetEffectiveSetting(parent.NeutralizeAfterOneTag, child.NeutralizeAfterOneTag),
				neutralizeAfterTenTags: GetEffectiveSetting(parent.NeutralizeAfterTenTags, child.NeutralizeAfterTenTags),
				hasTeamTags: GetEffectiveSetting(parent.HasTeamTags, child.HasTeamTags),
				hasMedicMode: GetEffectiveSetting(parent.HasMedicMode, child.HasMedicMode),
				hasSlowTags: GetEffectiveSetting(parent.HasSlowTags, child.HasSlowTags),
				isHuntThePrey: GetEffectiveSetting(parent.IsHuntThePrey, child.IsHuntThePrey),
				reverseHuntDirection: GetEffectiveSetting(parent.ReverseHuntDirection, child.ReverseHuntDirection),
				hasContestedZones: GetEffectiveSetting(parent.HasContestedZones, child.HasContestedZones),
				zonesCanHaveTeams: GetEffectiveSetting(parent.ZonesCanHaveTeams, child.ZonesCanHaveTeams),
				hasUnneutralizeZones: GetEffectiveSetting(parent.HasUnneutralizeZones, child.HasUnneutralizeZones),
				hasHealingZones: GetEffectiveSetting(parent.HasHealingZones, child.HasHealingZones),
				hasHostileZones: GetEffectiveSetting(parent.HasHostileZones, child.HasHostileZones)
			);
		}

		// EffectiveGameSettings -> GameSettings
		public static implicit operator GameSettings(EffectiveGameSettings effectiveGameSettings)
		{
			return effectiveGameSettings.Effective;
		}

		public EffectiveGameSettings Parent { get; private set; }
		public GameSettings Default { get; private set; }
		public GameSettings User { get; private set; }
		public GameSettings Effective { get; private set; }

		private static GameSettings GetEffectiveSettings(EffectiveGameSettings parentEffectiveSettings, GameSettings defaultSettings, GameSettings userSettings)
		{
			return GetEffectiveSettings(parentEffectiveSettings, GetEffectiveSettings(defaultSettings, userSettings));
		}

		private static GameSetting<T> GetEffectiveSetting<T>(GameSetting<T> parent, GameSetting<T> child) where T : struct
		{
			if (!parent.IsSet && !child.IsSet) return new GameSetting<T>();
			if (!parent.IsSet) return child.Value;
			if (!child.IsSet) return parent.Value;
			if (parent.IsLocked || child.IsInherited) return parent.Value;
			return child.Value;
		}
	}
}
