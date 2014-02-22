using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Xml;

namespace LazerTagHostLibrary
{
	[ImmutableObject(true)]
	public class GameType1 : IImmutableObject
	{
		public Guid Guid { get; private set; }
		public string Name { get; private set; }
		public LazerTagString ShortName { get; private set; }
		public string Description { get; private set; }
		public UInt16 AnnounceGameCommandCode { get; private set; }
		public Range<int> TeamsRange { get; private set; }
		public Range<int> SquadsRange { get; private set; } 
		public Range<int> PlayersRange { get; private set; }
		public BinaryCodedDecimal DefaultGameLengthMinutes { get; private set; }
		public int GameLengthStepMinutes { get; private set; }
		public ScoringMethod ScoringMethod { get; private set; }
		public ReadOnlyCollection<PlayerRole> PlayerRoles { get; private set; }
		public ReadOnlyCollection<GameZone> GameZones { get; private set; }

		public sealed class Builder : ImmutableObjectBuilder<GameType1>
		{
			public Guid Guid
			{
				get { return _instance.Guid; }
				set { _instance.Guid = value; }
			}

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

			public UInt16 AnnounceGameCommandCode
			{
				get { return _instance.AnnounceGameCommandCode; }
				set { _instance.AnnounceGameCommandCode = value; }
			}

			public Range<int> TeamsRange
			{
				get { return _instance.TeamsRange; }
				set { _instance.TeamsRange = value; }
			}

			public Range<int> SquadsRange
			{
				get { return _instance.SquadsRange; }
				set { _instance.SquadsRange = value; }
			}

			public Range<int> PlayersRange
			{
				get { return _instance.PlayersRange; }
				set { _instance.PlayersRange = value; }
			}

			public BinaryCodedDecimal DefaultGameLengthMinutes
			{
				get { return _instance.DefaultGameLengthMinutes; }
				set { _instance.DefaultGameLengthMinutes = value; }
			}

			public int GameLengthStepMinutes
			{
				get { return _instance.GameLengthStepMinutes; }
				set { _instance.GameLengthStepMinutes = value; }
			}

			public ScoringMethod ScoringMethod
			{
				get { return _instance.ScoringMethod; }
				set { _instance.ScoringMethod = value; }
			}

			public IList<PlayerRole> PlayerRoles
			{
				get { return _instance.PlayerRoles; }
				set { _instance.PlayerRoles = new ReadOnlyCollection<PlayerRole>(value); }
			}

			public IList<GameZone> GameZones
			{
				get { return _instance.GameZones; }
				set { _instance.GameZones = new ReadOnlyCollection<GameZone>(value); }
			}

			protected override GameType1 Build()
			{
				return _instance;
			}

			private readonly GameType1 _instance = new GameType1();
		}
	}

	public enum ScoringMethod
	{
		Standard,
		Kings,
		OwnTheZone,
	}

	[ImmutableObject(true)]
	public class GameTypes1 : ReadOnlyCollection<GameType1>
	{
		public GameTypes1(string fileName)
			: base(LoadFromXml(fileName))
		{

		}

		private static IList<GameType1> LoadFromXml(string xmlFileName)
		{
			var loadedGameTypes = new List<GameType1>();

			try
			{
				var xmlDocument = new XmlDocument();
				xmlDocument.Load(xmlFileName);

				XmlSerializer.VerifyFileInfo(xmlDocument, "GameTypes", new Version(1, 0));

				var gameTypeNodes = xmlDocument.SelectNodes("/LazerSwarm/GameTypes/GameType");
				if (gameTypeNodes == null) throw new NullReferenceException("gameTypeNodes");
				foreach (XmlNode gameTypeNode in gameTypeNodes)
				{
					if (gameTypeNode.Attributes == null) throw new NullReferenceException("gameTypeNode.Attributes");
					var nameAttribute = gameTypeNode.Attributes["Name"];
					if (nameAttribute == null) throw new NullReferenceException("nameAttribute");

					var teamCount = XmlSerializer.GetNodeTextInt(gameTypeNode, "TeamCount");

					var gameSettings = new GameSettings(
						tagsAmount: (BinaryCodedDecimal) XmlSerializer.GetNodeTextInt(gameTypeNode, "DefaultTags"),
						reloadsAmount: (BinaryCodedDecimal) XmlSerializer.GetNodeTextInt(gameTypeNode, "DefaultReloads"),
						shieldsAmount: (BinaryCodedDecimal) XmlSerializer.GetNodeTextInt(gameTypeNode, "DefaultShields"),
						megaAmount: (BinaryCodedDecimal) XmlSerializer.GetNodeTextInt(gameTypeNode, "DefaultMega"),
						neutralizeAfterOneTag: XmlSerializer.GetNodeTextBool(gameTypeNode, "NeutralizePlayersTaggedInZone"),
						hasTeamTags: XmlSerializer.GetNodeTextBool(gameTypeNode, "DefaultTeamTags"),
						hasMedicMode: XmlSerializer.GetNodeTextBool(gameTypeNode, "DefaultMedicMode"),
						isHuntThePrey: XmlSerializer.GetNodeTextBool(gameTypeNode, "HuntThePrey"),
						hasContestedZones: XmlSerializer.GetNodeTextBool(gameTypeNode, "Zones"));

					PlayerRole playerRole = new PlayerRole.Builder
					{
						Name = "Player",
						IsOptional = false,
						PlayerRange = new Range<int>(-1,-1),
						GameSettings = gameSettings,
					};

					GameZone[] gameZones;

					if (gameSettings.HasContestedZones)
					{
						gameZones = new[]
						{
							(GameZone) new GameZone.Builder
							{
								Name = "Contested Zone",
								ZoneType = GameZoneType.Contested,
								IsHostile = false,
								TeamNumber = 0,
								BeaconFrequencyMilliseconds = 500,
							}
						};
					}
					else
					{
						gameZones = new GameZone[0];
					}

					GameType1 gameType = new GameType1.Builder
					{
						Guid = Guid.NewGuid(), // TODO: read this from the XML
						Name = XmlSerializer.GetNodeLocalizedText(gameTypeNode, "DisplayName"),
						ShortName = (LazerTagString) nameAttribute.Value,
						AnnounceGameCommandCode = (UInt16) XmlSerializer.GetNodeTextInt(gameTypeNode, "AnnounceGameCommandCode"),
						TeamsRange = new Range<int>(teamCount, teamCount),
						PlayersRange = new Range<int>(2, 24),
						DefaultGameLengthMinutes =
							(BinaryCodedDecimal) XmlSerializer.GetNodeTextInt(gameTypeNode, "DefaultGameTimeMinutes"),
						GameLengthStepMinutes = XmlSerializer.GetNodeTextInt(gameTypeNode, "GameTimeStepMinutes"),
						PlayerRoles = new[] {playerRole},
						GameZones = gameZones,
					};

					loadedGameTypes.Add(gameType);
				}
			}
			catch (Exception ex)
			{
				Log.Add(Log.Severity.Error, "GameTypes.LoadFromXml() failed.", ex);
				throw;
			}

			return loadedGameTypes;
		}
	}
}
