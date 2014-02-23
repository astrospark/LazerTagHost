using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace LazerTagHostLibrary
{
	[ImmutableObject(true)]
	public class GameType1
	{
		public GameType1(
			Guid guid = new Guid(),
			string name = null,
			LazerTagString shortName = null,
			string description = null,
			Range<int> teamsRange = null,
			Range<int> playersRange = null,
			UInt16 announceGameCommandCode = 0,
			BinaryCodedDecimal gameLengthMinutes = new BinaryCodedDecimal(),
			int gameLengthStepMinutes = 0,
			GameSettings gameSettings = null,
			GameSettings gameSettingsSolo = null,
			GameSettings gameSettingsTeam = null,
			IList<GameTypeTeam> teams = null,
			IList<GameTypeRole> roles = null,
			IList<GameTypeZone> zones = null,
			ScoringMethod scoringMethod = 0)
		{
			Guid = guid;
			Name = name;
			ShortName = shortName;
			Description = description;
			TeamsRange = teamsRange;
			PlayersRange = playersRange;
			AnnounceGameCommandCode = announceGameCommandCode;
			GameLengthMinutes = gameLengthMinutes;
			GameLengthStepMinutes = gameLengthStepMinutes;
			GameSettings = gameSettings;
			GameSettingsSolo = gameSettingsSolo;
			GameSettingsTeam = gameSettingsTeam;
			Teams = (teams == null)
				? new ReadOnlyCollection<GameTypeTeam>(new GameTypeTeam[] {})
				: new ReadOnlyCollection<GameTypeTeam>(teams);
			Roles = (roles == null)
				? new ReadOnlyCollection<GameTypeRole>(new GameTypeRole[] { })
				: new ReadOnlyCollection<GameTypeRole>(roles);
			Zones = (zones == null)
				? new ReadOnlyCollection<GameTypeZone>(new GameTypeZone[] { })
				: new ReadOnlyCollection<GameTypeZone>(zones);
			ScoringMethod = scoringMethod;
		}

		public Guid Guid { get; private set; }

		public string Name { get; private set; }
		public LazerTagString ShortName { get; private set; }
		public string Description { get; private set; }

		public Range<int> TeamsRange { get; private set; }
		public Range<int> PlayersRange { get; private set; }

		public UInt16 AnnounceGameCommandCode { get; private set; }
		public BinaryCodedDecimal GameLengthMinutes { get; private set; }
		public int GameLengthStepMinutes { get; private set; }
		public GameSettings GameSettings { get; private set; }
		public GameSettings GameSettingsSolo { get; private set; }
		public GameSettings GameSettingsTeam { get; private set; }

		public ReadOnlyCollection<GameTypeTeam> Teams { get; private set; } 
		public ReadOnlyCollection<GameTypeRole> Roles { get; private set; }
		public ReadOnlyCollection<GameTypeZone> Zones { get; private set; }
	
		public ScoringMethod ScoringMethod { get; private set; }
	}

	public enum ScoringMethod
	{
		Standard,
		Kings,
		OwnTheZone,
	}
}
