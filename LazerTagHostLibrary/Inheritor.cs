using System.ComponentModel;

namespace LazerTagHostLibrary
{
	public interface IInheritor<out T>
	{
		T Parent { get; }
		T Effective { get; }
		bool IsInherited { get; }
	}

	[ImmutableObject(true)]
	public class Inheritor<T> : IInheritor<T> where T : struct
	{
		public Inheritor(T parent, T child, bool isInherited)
		{
			Parent = parent;
			Child = child;
			IsInherited = isInherited;
		}

		public T Parent { get; private set; }
		public T Child { get; private set; }
		public bool IsInherited { get; private set; }

		public T Effective
		{
			get { return IsInherited ? Parent : Child; }
		}
	}
}
