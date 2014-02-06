using System;

namespace LazerTagHostLibrary.Games
{
	public static class GameFactory
	{
		public static GameBase CreateGame(GameType gameType)
		{
			switch (gameType)
			{
				case GameType.LazerTag:
				case GameType.TeamLazerTag:
				case GameType.CustomLazerTag:
				case GameType.CustomLazerTagTwoTeams:
				case GameType.CustomLazerTagThreeTeams:
				case GameType.HideAndSeek:
				case GameType.TagMasterHideAndSeek:
				case GameType.HuntThePrey:
				case GameType.TagMasterHuntThePrey:
				case GameType.KingsTwoTeams:
				case GameType.KingsThreeTeams:
				case GameType.OwnTheZone:
				case GameType.OwnTheZoneTwoTeams:
				case GameType.OwnTheZoneThreeTeams:
				case GameType.HuntTheTagMaster:
				case GameType.BigTeamHuntTheTagMaster:
				case GameType.HuntTheTagMasterTwoTeams:
				case GameType.Respawn:
				case GameType.RespawnTwoTeams:
				case GameType.RespawnThreeTeams:
				case GameType.OneOnOne:
				case GameType.Survival:
				case GameType.SurvivalTwoTeams:
				case GameType.SurvivalThreeTeams:
					return new GameBase();
				default:
					throw new ArgumentException("Unrecognized gameType.", "gameType");
			}
		}
	}
}
