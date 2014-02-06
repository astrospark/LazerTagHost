using System.ComponentModel;

namespace LazerTagHostLibrary
{
	[ImmutableObject(true)]
	public class Range<T>
	{
		public Range(T minimum, T maximum)
		{
			Minimum = minimum;
			Maximum = maximum;
		}

		public T Minimum { get; private set; }
		public T Maximum { get; private set; }
	}
}
