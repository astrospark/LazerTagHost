using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Xml;

namespace LazerTagHostLibrary
{
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
						tagsAmount: (BinaryCodedDecimal)XmlSerializer.GetNodeTextInt(gameTypeNode, "DefaultTags"),
						reloadsAmount: (BinaryCodedDecimal)XmlSerializer.GetNodeTextInt(gameTypeNode, "DefaultReloads"),
						shieldsAmount: (BinaryCodedDecimal)XmlSerializer.GetNodeTextInt(gameTypeNode, "DefaultShields"),
						megaAmount: (BinaryCodedDecimal)XmlSerializer.GetNodeTextInt(gameTypeNode, "DefaultMega"),
						neutralizeAfterOneTag: XmlSerializer.GetNodeTextBool(gameTypeNode, "NeutralizePlayersTaggedInZone"),
						hasTeamTags: XmlSerializer.GetNodeTextBool(gameTypeNode, "DefaultTeamTags"),
						hasMedicMode: XmlSerializer.GetNodeTextBool(gameTypeNode, "DefaultMedicMode"),
						isHuntThePrey: XmlSerializer.GetNodeTextBool(gameTypeNode, "HuntThePrey"),
						hasContestedZones: XmlSerializer.GetNodeTextBool(gameTypeNode, "Zones"));

					var roles = new[]
					{
						new GameTypeRole(
							name: "Player",
							gameSettings: gameSettings),
					};

					GameTypeZone[] zones;

					if (gameSettings.HasContestedZones)
					{
						zones = new[]
						{
							new GameTypeZone(
								name: "Contested Zone",
								zoneType: GameZoneType.Contested,
								isHostile: false,
								teamNumber: 0,
								beaconFrequencyMilliseconds: 500),
						};
					}
					else
					{
						zones = new GameTypeZone[0];
					}

					var gameType = new GameType1(
						guid: Guid.NewGuid(), // TODO: read this from the XML
						name: XmlSerializer.GetNodeLocalizedText(gameTypeNode, "DisplayName"),
						shortName: (LazerTagString)nameAttribute.Value,
						announceGameCommandCode: (UInt16)XmlSerializer.GetNodeTextInt(gameTypeNode, "AnnounceGameCommandCode"),
						teamsRange: new Range<int>(teamCount, teamCount),
						playersRange: new Range<int>(2, 24),
						gameLengthMinutes: (BinaryCodedDecimal)XmlSerializer.GetNodeTextInt(gameTypeNode, "DefaultGameTimeMinutes"),
						gameLengthStepMinutes: XmlSerializer.GetNodeTextInt(gameTypeNode, "GameTimeStepMinutes"),
						roles: new ReadOnlyCollection<GameTypeRole>(roles),
						zones: zones);

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
