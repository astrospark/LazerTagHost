using System.ComponentModel;

namespace LazerTagHostLibrary
{
	[ImmutableObject(true)]
	public class GameSettings
	{
		public GameSettings(
			GameSettings parent = null,
			GameSetting<BinaryCodedDecimal> tagsAmount = null,
			GameSetting<BinaryCodedDecimal> reloadsAmount = null,
			GameSetting<BinaryCodedDecimal> shieldsAmount = null,
			GameSetting<BinaryCodedDecimal> megaAmount = null,
			GameSetting<bool> neutralizeAfterOneTag = null,
			GameSetting<bool> neutralizeAfterTenTags = null,
			GameSetting<bool> hasTeamTags = null,
			GameSetting<bool> hasMedicMode = null,
			GameSetting<bool> hasSlowTags = null,
			GameSetting<bool> isHuntThePrey = null,
			GameSetting<bool> reverseHuntDirection = null,
			GameSetting<bool> hasContestedZones = null,
			GameSetting<bool> zonesCanHaveTeams = null,
			GameSetting<bool> hasUnneutralizeZones = null,
			GameSetting<bool> hasHealingZones = null,
			GameSetting<bool> hasHostileZones = null,
			GameSettings gameSettings = null)
		{
			if (gameSettings != null)
			{
				Parent = gameSettings.Parent;
				TagsAmount = gameSettings.TagsAmount;
				ReloadsAmount = gameSettings.ReloadsAmount;
				ShieldsAmount = gameSettings.ShieldsAmount;
				MegaAmount = gameSettings.MegaAmount;
				NeutralizeAfterOneTag = gameSettings.NeutralizeAfterOneTag;
				NeutralizeAfterTenTags = gameSettings.NeutralizeAfterTenTags;
				HasTeamTags = gameSettings.HasTeamTags;
				HasMedicMode = gameSettings.HasMedicMode;
				HasSlowTags = gameSettings.HasSlowTags;
				IsHuntThePrey = gameSettings.IsHuntThePrey;
				ReverseHuntDirection = gameSettings.ReverseHuntDirection;
				HasContestedZones = gameSettings.HasContestedZones;
				ZonesCanHaveTeams = gameSettings.ZonesCanHaveTeams;
				HasUnneutralizeZones = gameSettings.HasUnneutralizeZones;
				HasHealingZones = gameSettings.HasHealingZones;
				HasHostileZones = gameSettings.HasHostileZones;
			}

			Parent = parent ?? Parent;
			TagsAmount = (tagsAmount == null ? null : tagsAmount.Clone()) ?? TagsAmount;
			ReloadsAmount = (reloadsAmount == null ? null : reloadsAmount.Clone()) ?? ReloadsAmount;
			ShieldsAmount = (shieldsAmount == null ? null : shieldsAmount.Clone()) ?? ShieldsAmount;
			MegaAmount = (megaAmount == null ? null : megaAmount.Clone()) ?? MegaAmount;
			NeutralizeAfterOneTag = (neutralizeAfterOneTag == null ? null : neutralizeAfterOneTag.Clone()) ?? NeutralizeAfterOneTag;
			NeutralizeAfterTenTags = (neutralizeAfterTenTags == null ? null : neutralizeAfterTenTags.Clone()) ?? NeutralizeAfterTenTags;
			HasTeamTags = (hasTeamTags == null ? null : hasTeamTags.Clone()) ?? HasTeamTags;
			HasMedicMode = (hasMedicMode == null ? null : hasMedicMode.Clone()) ?? HasMedicMode;
			HasSlowTags = (hasSlowTags == null ? null : hasSlowTags.Clone()) ?? HasSlowTags;
			IsHuntThePrey = (isHuntThePrey == null ? null : isHuntThePrey.Clone()) ?? IsHuntThePrey;
			ReverseHuntDirection = (reverseHuntDirection == null ? null : reverseHuntDirection.Clone()) ?? ReverseHuntDirection;
			HasContestedZones = (hasContestedZones == null ? null : hasContestedZones.Clone()) ?? HasContestedZones;
			ZonesCanHaveTeams = (zonesCanHaveTeams == null ? null : zonesCanHaveTeams.Clone()) ?? ZonesCanHaveTeams;
			HasUnneutralizeZones = (hasUnneutralizeZones == null ? null : hasUnneutralizeZones.Clone()) ?? HasUnneutralizeZones;
			HasHealingZones = (hasHealingZones == null ? null : hasHealingZones.Clone()) ?? HasHealingZones;
			HasHostileZones = (hasHostileZones == null ? null : hasHostileZones.Clone()) ?? HasHostileZones;
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

		public GameSettings Parent { get; private set; }

		public GameSettings Effective
		{
			get
			{
				if (Parent == null) return this;

				var parent = Parent.Effective;

				return new GameSettings(
					parent: Parent,
					tagsAmount: TagsAmount.Effective(parent.TagsAmount),
					reloadsAmount: ReloadsAmount.Effective(parent.ReloadsAmount),
					shieldsAmount: ShieldsAmount.Effective(parent.ShieldsAmount),
					megaAmount: MegaAmount.Effective(parent.MegaAmount),
					neutralizeAfterOneTag: NeutralizeAfterOneTag.Effective(parent.NeutralizeAfterOneTag),
					neutralizeAfterTenTags: NeutralizeAfterTenTags.Effective(parent.NeutralizeAfterTenTags),
					hasTeamTags: HasTeamTags.Effective(parent.HasTeamTags),
					hasMedicMode: HasMedicMode.Effective(parent.HasMedicMode),
					hasSlowTags: HasSlowTags.Effective(parent.HasSlowTags),
					isHuntThePrey: IsHuntThePrey.Effective(parent.IsHuntThePrey),
					reverseHuntDirection: ReverseHuntDirection.Effective(parent.ReverseHuntDirection),
					hasContestedZones: HasContestedZones.Effective(parent.HasContestedZones),
					zonesCanHaveTeams: ZonesCanHaveTeams.Effective(parent.ZonesCanHaveTeams),
					hasUnneutralizeZones: HasUnneutralizeZones.Effective(parent.HasUnneutralizeZones),
					hasHealingZones: HasHealingZones.Effective(parent.HasHealingZones),
					hasHostileZones: HasHostileZones.Effective(parent.HasHostileZones));
			}
		}
	}

	[ImmutableObject(true)]
	public class GameSetting<T> where T: struct 
	{
		public GameSetting(
			bool isInherited = false,
			bool isLocked = false,
			T value = default(T))
		{
			IsInherited = isInherited;
			IsLocked = isLocked;
			Value = value;
		}

		public GameSetting(GameSetting<T> gameSetting) : this(
			isInherited: gameSetting.IsInherited,
			isLocked: gameSetting.IsLocked,
			value: gameSetting.Value)
		{

		}

		public GameSetting<T> Clone()
		{
			return new GameSetting<T>(this);
		}

		public static implicit operator T(GameSetting<T> gameSetting)
		{
			return gameSetting.Value;
		}

		public static implicit operator GameSetting<T>(T value)
		{
			return new GameSetting<T>(value: value);
		}

		public bool IsInherited { get; private set; }
		public bool IsLocked { get; private set; }
		public T Value { get; private set; }

		public GameSetting<T> Effective(GameSetting<T> parentSetting)
		{
			return IsInherited ? parentSetting : this;
		}
	}
}
