using System.ComponentModel;

namespace LazerTagHostLibrary
{
	[ImmutableObject(true)]
	public interface IImmutableObject
	{

	}

	public abstract class ImmutableObjectBuilder<T> where T : IImmutableObject
	{
		public static implicit operator T(ImmutableObjectBuilder<T> builder)
		{
			return builder.Build();
		}

		protected abstract T Build();
	}
}
