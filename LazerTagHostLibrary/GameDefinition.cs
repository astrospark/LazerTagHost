namespace LazerTagHostLibrary
{
	public struct GameDefinition
	{
		public HostGun.CommandCode GameType { get; set; }
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
		public bool Hunt { get; set; }
		public bool HuntDirection { get; set; }
		public bool Zones { get; set; }
		public bool TeamZones { get; set; }
		public bool NeutralizeTaggedPlayers { get; set; }
		public bool ZonesRevivePlayers { get; set; }
		public bool HospitalZones { get; set; }
		public bool ZonesTagPlayers { get; set; }
		public int TeamCount { get; set; }

		private char[] _name;
		public char[] Name
		{
			get { return _name; }
			set
			{
				if (value == null)
				{
					_name = null;
					return;
				}

				_name = new char[4];
				for (var i = 0; i < 4; i++)
				{
					_name[i] = (value.Length > i) ? value[i] : ' ';
				}
			}
		}

		public int CountdownTimeSeconds { get; set; }
	}
}
