using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
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

				var fileInfoNode = xmlDocument.SelectSingleNode("/LazerSwarm/FileInfo");
				if (fileInfoNode == null) throw new NullReferenceException("fileInfoNode");
				if (fileInfoNode.Attributes == null) throw new NullReferenceException("fileInfoNode.Attributes");
				var fileInfoVersionAttribute = fileInfoNode.Attributes["Version"];
				if (fileInfoVersionAttribute == null) throw new NullReferenceException("versionAttribute");
				var fileInfoVersion = new Version(fileInfoVersionAttribute.Value);
				if (fileInfoVersion > new Version(1, 0))
				{
					throw new FileFormatException(string.Format("Unsupported FileInfo version ({0}).", fileInfoVersion));
				}

				var fileTypeNode = fileInfoNode.SelectSingleNode("./FileType");
				if (fileTypeNode == null) throw new NullReferenceException("fileTypeNode");
				var fileType = fileTypeNode.InnerText;
				if (fileType != "GameTypes")
				{
					throw new FileFormatException(string.Format("Incorrect FileType ({0}). Expected \"GameTypes\".", fileType));
				}

				var fileTypeVersionNode = fileInfoNode.SelectSingleNode("./FileTypeVersion");
				if (fileTypeVersionNode == null) throw new NullReferenceException("fileTypeVersionNode");
				var fileTypeVersion = new Version(fileTypeVersionNode.InnerText);
				if (fileTypeVersion > new Version(1, 0))
				{
					throw new FileFormatException(string.Format("Unsupported FileTypeVersion ({0}).", fileInfoVersion));
				}

				var gameTypeNodes = xmlDocument.SelectNodes("/LazerSwarm/GameTypes/GameType");
				if (gameTypeNodes == null) throw new NullReferenceException("gameTypeNodes");
				foreach (XmlNode gameTypeNode in gameTypeNodes)
				{
					var gameTypeInfo = new GameTypeInfo();

					if (gameTypeNode.Attributes == null) throw new NullReferenceException("gameTypeNode.Attributes");
					var nameAttribute = gameTypeNode.Attributes["Name"];
					if (nameAttribute == null) throw new NullReferenceException("nameAttribute");
					gameTypeInfo.Type = TypeFromName(nameAttribute.Value);
					gameTypeInfo.Name = nameAttribute.Value;

					gameTypeInfo.DisplayName = GetNodeLocalizedText(gameTypeNode, "DisplayName");
					gameTypeInfo.CommandCode = (HostGun.CommandCode)GetNodeTextInt(gameTypeNode, "AnnounceGameCommandCode");
					gameTypeInfo.TeamCount = GetNodeTextInt(gameTypeNode, "TeamCount");
					gameTypeInfo.GameTimeStepMinutes = GetNodeTextInt(gameTypeNode, "GameTimeStepMinutes");
					gameTypeInfo.HuntThePrey = GetNodeTextBool(gameTypeNode, "HuntThePrey");
					gameTypeInfo.Zones = GetNodeTextBool(gameTypeNode, "Zones");
					gameTypeInfo.NeutralizePlayersTaggedInZone = GetNodeTextBool(gameTypeNode, "NeutralizePlayersTaggedInZone");
					gameTypeInfo.DefaultTeamTags = GetNodeTextBool(gameTypeNode, "DefaultTeamTags");
					gameTypeInfo.DefaultMedicMode = GetNodeTextBool(gameTypeNode, "DefaultMedicMode");
					gameTypeInfo.DefaultGameTimeMinutes = GetNodeTextInt(gameTypeNode, "DefaultGameTimeMinutes");
					gameTypeInfo.DefaultReloads = GetNodeTextInt(gameTypeNode, "DefaultReloads");
					gameTypeInfo.DefaultMega = GetNodeTextInt(gameTypeNode, "DefaultMega");
					gameTypeInfo.DefaultShields = GetNodeTextInt(gameTypeNode, "DefaultShields");
					gameTypeInfo.DefaultTags = GetNodeTextInt(gameTypeNode, "DefaultTags");

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

		private static string GetNodeText(XmlNode parentNode, string nodeName)
		{
			var node = parentNode.SelectSingleNode(string.Format("./{0}", nodeName));
			if (node == null) throw new NullReferenceException("node");
			return node.InnerText;
		}

		private static string GetNodeLocalizedText(XmlNode parentNode, string nodeName, CultureInfo cultureInfo = null)
		{
			var node = parentNode.SelectSingleNode(string.Format("./{0}", nodeName));
			if (node == null) throw new NullReferenceException("node");

			var localizedTextNode = SelectLocalizedTextNode(node);
			if (localizedTextNode == null) throw new NullReferenceException("localizedTextNode");

			return localizedTextNode.InnerText;
		}

		private static int GetNodeTextInt(XmlNode parentNode, string nodeName)
		{
			return StringToInt(GetNodeText(parentNode, nodeName));
		}

		private static bool GetNodeTextBool(XmlNode parentNode, string nodeName)
		{
			return Convert.ToBoolean(GetNodeText(parentNode, nodeName));
		}

		private static XmlNode SelectLocalizedTextNode(XmlNode parentNode, CultureInfo cultureInfo = null)
		{
			XmlNode localizedTextNode = null;
			if (cultureInfo != null)
			{
				localizedTextNode = parentNode.SelectSingleNode(string.Format("./LocalizedText[@Culture='{0}']",
				                                                              cultureInfo.Name));
			}

			// Fall back to CurrentUICulture
			if (localizedTextNode == null)
			{
				localizedTextNode = parentNode.SelectSingleNode(string.Format("./LocalizedText[@Culture='{0}']",
				                                                              Thread.CurrentThread.CurrentUICulture.Name));
			}

			// Fall back to first or only LocalizedText node
			if (localizedTextNode == null)
			{
				localizedTextNode = parentNode.SelectSingleNode("./LocalizedText");
			}

			return localizedTextNode;
		}

		private static int StringToInt(string input)
		{
			var regex = new Regex(@"^\s*0x([0-9a-fA-F]{1,8})\s*$");
			var match = regex.Match(input);
			if (match.Success)
			{
				var hexChars = match.Captures[0].Value;
				return Convert.ToInt32(hexChars, 16);
			}

			return Convert.ToInt32(input);
		}
	}

	public struct GameTypeInfo
	{
		public GameType Type { get; set; }
		public string Name { get; set; }
		public string DisplayName { get; set; }
		public HostGun.CommandCode CommandCode { get; set; }
		public int TeamCount { get; set; }
		public int GameTimeStepMinutes { get; set; }
		public bool HuntThePrey { get; set; }
		public bool Zones { get; set; }
		public bool TeamZones { get; set; }
		public bool NeutralizePlayersTaggedInZone { get; set; }
		public bool ZonesRevivePlayers { get; set; }
		public bool HospitalZones { get; set; }
		public bool ZonesTagPlayers { get; set; }
		public bool DefaultTeamTags { get; set; }
		public bool DefaultMedicMode { get; set; }
		public int DefaultGameTimeMinutes { get; set; }
		public int DefaultReloads { get; set; }
		public int DefaultMega { get; set; }
		public int DefaultShields { get; set; }
		public int DefaultTags { get; set; }
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