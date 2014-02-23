using System.ComponentModel;

namespace LazerTagHostLibrary
{
	[ImmutableObject(true)]
	public struct GameSetting<T> where T : struct
	{
		public GameSetting(
			T value,
			bool isInherited = false,
			bool isLocked = false)
			: this()
		{
			Value = value;
			IsInherited = isInherited;
			IsLocked = isLocked;
		}

		public GameSetting(GameSetting<T> gameSetting)
			: this(
				gameSetting.Value,
				gameSetting.IsInherited,
				gameSetting.IsLocked)
		{

		}

		public GameSetting<T> Clone()
		{
			return new GameSetting<T>(this);
		}

		// GameSetting -> T
		public static implicit operator T(GameSetting<T> gameSetting)
		{
			return gameSetting.Value;
		}

		// T -> GameSetting
		public static implicit operator GameSetting<T>(T value)
		{
			return new GameSetting<T>(value);
		}

		private T _value;
		public T Value
		{
			get { return _value; }
			private set
			{
				_value = value;
				IsSet = true;
			}
		}

		public bool IsSet { get; private set; }
		public bool IsInherited { get; private set; }
		public bool IsLocked { get; private set; }
	}
}
