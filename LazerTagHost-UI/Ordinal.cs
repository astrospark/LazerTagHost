namespace LazerTagHostUI
{
	class Ordinal
	{
		public static string FromCardinal(int cardinal)
		{
			string formatString;

			switch (cardinal % 10)
			{
				case 1:
					formatString = "{0}st";
					break;
				case 2:
					formatString = "{0}nd";
					break;
				case 3:
					formatString = "{0}rd";
					break;
				default:
					formatString = "{0}th";
					break;
			}

			switch (cardinal % 100)
			{
				case 11:
				case 12:
				case 13:
					formatString = "{0}th";
					break;
			}

			if (cardinal < 1) formatString = "{0}";

			return string.Format(formatString, cardinal);
		}
	}

	public static class OrdinalFilter
	{
		public static string ToOrdinal(int cardinal)
		{
			return Ordinal.FromCardinal(cardinal);
		}
	}
}
