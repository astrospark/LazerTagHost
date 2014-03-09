using System;
using System.Xml;
using System.Collections.Generic;

namespace LazerTagHostLibrary
{
	public class GameTypes
	{
		static GameTypes()
		{
			LoadFromXml("GameTypes.xml");
		}

		public static GameTypeInfo GetInfo(GameType gameType)
		{
			return _gameTypesInfo[gameType];
		}

		public static GameTypeInfo GetInfo(string name)
		{
			return _gameTypesInfo[TypeFromName(name)];
		}

		private static Dictionary<GameType, GameTypeInfo> _gameTypesInfo;

		private static GameType TypeFromName(string name)
		{
			switch (name)
			{
				case "LTAG":
					return GameType.LazerTag;
				case "TTAG":
					return GameType.TeamLazerTag;
				case "CUST":
					return GameType.CustomLazerTag;
				case "2TMS":
					return GameType.CustomLazerTagTwoTeams;
				case "3TMS":
					return GameType.CustomLazerTagThreeTeams;
				case "HDSK":
					return GameType.HideAndSeek;
				case "TMHS":
					return GameType.TagMasterHideAndSeek;
				case "HUNT":
					return GameType.HuntThePrey;
				case "TMHP":
					return GameType.TagMasterHuntThePrey;
				case "2KNG":
					return GameType.KingsTwoTeams;
				case "3KNG":
					return GameType.KingsThreeTeams;
				case "OWNZ":
					return GameType.OwnTheZone;
				case "2TOZ":
					return GameType.OwnTheZoneTwoTeams;
				case "3TOZ":
					return GameType.OwnTheZoneThreeTeams;
				case "TAGM":
					return GameType.HuntTheTagMaster;
				case "TMIG":
					return GameType.BigTeamHuntTheTagMaster;
				case "2TTM":
					return GameType.HuntTheTagMasterTwoTeams;
				case "RESP":
					return GameType.Respawn;
				case "2TRS":
					return GameType.RespawnTwoTeams;
				case "3TRS":
					return GameType.RespawnThreeTeams;
				case "1ON1":
					return GameType.OneOnOne;
				case "SURV":
					return GameType.Survival;
				case "2SRV":
					return GameType.SurvivalTwoTeams;
				case "3SRV":
					return GameType.SurvivalThreeTeams;
				default:
					return GameType.LazerTag;
			}
		}

		private static void LoadFromXml(string xmlFileName)
		{
			var loadedGameTypes = new Dictionary<GameType, GameTypeInfo>();

			try
			{
				var xmlDocument = new XmlDocument();
				xmlDocument.Load(xmlFileName);

				XmlSerializer.VerifyFileInfo(xmlDocument, "GameTypes", new Version(1, 0));

				var gameTypeNodes = xmlDocument.SelectNodes("/LazerSwarm/GameTypes/GameType");
				if (gameTypeNodes == null) throw new NullReferenceException("gameTypeNodes");
				foreach (XmlNode gameTypeNode in gameTypeNodes)
				{
					var gameTypeInfo = new GameTypeInfo();

					if (gameTypeNode.Attributes == null) throw new NullReferenceException("gameTypeNode.Attributes");
					var nameAttribute = gameTypeNode.Attributes["Name"];
					if (nameAttribute == null) throw new NullReferenceException("nameAttribute");
					gameTypeInfo.Type = TypeFromName(nameAttribute.Value);
					gameTypeInfo.Name = (LazerTagString) nameAttribute.Value;

					gameTypeInfo.DisplayName = XmlSerializer.GetNodeLocalizedText(gameTypeNode, "DisplayName");
					gameTypeInfo.PacketType = (PacketType)XmlSerializer.GetNodeTextInt(gameTypeNode, "AnnounceGameCommandCode");
					gameTypeInfo.TeamCount = XmlSerializer.GetNodeTextInt(gameTypeNode, "TeamCount");
					gameTypeInfo.GameTimeStepMinutes = XmlSerializer.GetNodeTextInt(gameTypeNode, "GameTimeStepMinutes");
					gameTypeInfo.DefaultSlowTags = XmlSerializer.GetNodeTextBool(gameTypeNode, "SlowTags");
					gameTypeInfo.HuntThePrey = XmlSerializer.GetNodeTextBool(gameTypeNode, "HuntThePrey");
					gameTypeInfo.ReverseHuntDirection = XmlSerializer.GetNodeTextBool(gameTypeNode, "ReverseHuntDirection");
					gameTypeInfo.Zones = XmlSerializer.GetNodeTextBool(gameTypeNode, "Zones");
					gameTypeInfo.NeutralizePlayersTaggedInZone = XmlSerializer.GetNodeTextBool(gameTypeNode, "NeutralizePlayersTaggedInZone");
					gameTypeInfo.DefaultTeamTags = XmlSerializer.GetNodeTextBool(gameTypeNode, "DefaultTeamTags");
					gameTypeInfo.DefaultMedicMode = XmlSerializer.GetNodeTextBool(gameTypeNode, "DefaultMedicMode");
					gameTypeInfo.DefaultGameTimeMinutes = XmlSerializer.GetNodeTextInt(gameTypeNode, "DefaultGameTimeMinutes");
					gameTypeInfo.DefaultReloads = XmlSerializer.GetNodeTextInt(gameTypeNode, "DefaultReloads");
					gameTypeInfo.DefaultMega = XmlSerializer.GetNodeTextInt(gameTypeNode, "DefaultMega");
					gameTypeInfo.DefaultShields = XmlSerializer.GetNodeTextInt(gameTypeNode, "DefaultShields");
					gameTypeInfo.DefaultTags = XmlSerializer.GetNodeTextInt(gameTypeNode, "DefaultTags");

					loadedGameTypes.Add(TypeFromName(gameTypeInfo.Name), gameTypeInfo);
				}
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				throw;
			}

			_gameTypesInfo = loadedGameTypes;
		}
	}

	public class GameTypeInfo
	{
		public GameType Type { get; set; }
		public LazerTagString Name { get; set; }
		public string DisplayName { get; set; }
		public PacketType PacketType { get; set; }
		public int TeamCount { get; set; }
		public int GameTimeStepMinutes { get; set; }
		public bool HuntThePrey { get; set; }
		public bool ReverseHuntDirection { get; set; }
		public bool Zones { get; set; }
		public bool TeamZones { get; set; }
		public bool NeutralizePlayersTaggedInZone { get; set; }
		public bool ZonesRevivePlayers { get; set; }
		public bool HospitalZones { get; set; }
		public bool ZonesTagPlayers { get; set; }
		public bool DefaultTeamTags { get; set; }
		public bool DefaultMedicMode { get; set; }
		public bool DefaultSlowTags { get; set; }
		public int DefaultGameTimeMinutes { get; set; }
		public int DefaultReloads { get; set; }
		public int DefaultMega { get; set; }
		public int DefaultShields { get; set; }
		public int DefaultTags { get; set; }
		public override string ToString()
		{
			return DisplayName;
		}
	}

	public enum GameType
	{
		LazerTag,
		TeamLazerTag,
		CustomLazerTag,
		CustomLazerTagTwoTeams,
		CustomLazerTagThreeTeams,
		HideAndSeek,
		TagMasterHideAndSeek,
		HuntThePrey,
		TagMasterHuntThePrey,
		KingsTwoTeams,
		KingsThreeTeams,
		OwnTheZone,
		OwnTheZoneTwoTeams,
		OwnTheZoneThreeTeams,
		HuntTheTagMaster,
		BigTeamHuntTheTagMaster,
		HuntTheTagMasterTwoTeams,
		Respawn,
		RespawnTwoTeams,
		RespawnThreeTeams,
		OneOnOne,
		Survival,
		SurvivalTwoTeams,
		SurvivalThreeTeams,
	}
}