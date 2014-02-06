namespace LazerTagHostLibrary
{
	public abstract class ImmutableObjectBuilder<T>
	{
		public static implicit operator T(ImmutableObjectBuilder<T> builder)
		{
			return builder.Build();
		}

		protected abstract T Build();
	}
}
