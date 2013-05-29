namespace LazerTagHostLibrary
{
	static class Tools
	{
		public static char[] GetCharArrayExactLength(string input, int length)
		{
			return GetCharArrayExactLength(input.ToCharArray(), length);
		}

		public static char[] GetCharArrayExactLength(char[] input, int length)
		{
			var output = new char[length];
			for (var i = 0; i < length; i++)
			{
				output[i] = (input != null && input.Length > i) ? input[i] : ' ';
			}
			return output;
		}
	}
}
