namespace LazerTagHostLibrary
{
	public class GameDefinition
	{
		public GameType GameType
		{
			get { return _gameTypeInfo.Type; }
			set { _gameTypeInfo = GameTypes.GetInfo(value); }
		}

		private GameTypeInfo _gameTypeInfo;
		public GameTypeInfo GameTypeInfo
		{
			get { return _gameTypeInfo; }
		}

		public byte GameId { get; set; }
		public int GameTimeMinutes { get; set; }
		public int Tags { get; set; }

		private int _reloads;
		public int Reloads
		{
			get { return _reloads; }
			set
			{
				_reloads = value;
				_unlimitedReloads = (_reloads == 0xff);
			}
		}

		public int Shields { get; set; }

		private int _mega;
		public int Mega
		{
			get
			{
				return _mega;
			}
			set
			{
				_mega = value;
				UnlimitedMega = (_mega == 0xff);
			}
		}

		public bool ExtendedTagging { get; set; }

		private bool _unlimitedReloads;
		public bool UnlimitedReloads
		{
			get { return _unlimitedReloads; }
			set
			{
				_unlimitedReloads = value;
				if (value)
				{
					_reloads = 0xff;
				}
				else
				{
					if (_reloads == 0xff) _reloads = 99;
				}
			}
		}

		private bool _unlimitedMega;
		public bool UnlimitedMega
		{
			get { return _unlimitedMega; }
			set
			{
				_unlimitedMega = value;
				if (value)
				{
					_mega = 0xff;
				}
				else
				{
					if (_mega == 0xff) _mega = 99;
				}
			}
		}

		public bool TeamTags { get; set; }
		public bool MedicMode { get; set; }
		public bool RapidTags { get; set; }
		public bool HuntDirection { get; set; }

		public bool IsZoneGame
		{
			get { return GameTypeInfo.Zones || GameTypeInfo.TeamZones || GameTypeInfo.ZonesRevivePlayers || GameTypeInfo.HospitalZones || GameTypeInfo.ZonesTagPlayers; }
		}

		public int TeamCount
		{
			get { return GameTypeInfo.TeamCount; }
		}

		public bool IsTeamGame
		{
			get { return (TeamCount > 1); }
		}

		private char[] _name;
		public char[] Name
		{
			get
			{
				if (_name == null & GameTypeInfo.PacketType == PacketType.AnnounceGameSpecial)
				{
					return Tools.GetCharArrayExactLength(GameTypeInfo.Name, 4);
				}
				return _name;
			}
			set
			{
				if (value == null)
				{
					_name = null;
					return;
				}
				_name = Tools.GetCharArrayExactLength(value, 4);
			}
		}

		public int CountdownTimeSeconds { get; set; }
	}
}
