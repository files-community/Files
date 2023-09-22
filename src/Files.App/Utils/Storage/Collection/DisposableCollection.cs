namespace Files.App.Utils.Storage.Collection
{
	public class DisposableCollection<T> : Collection<T?>, IDisposable where T : IDisposable?
	{
		protected bool disposed;

		public DisposableCollection(IList<T?> items) 
			: base(items)
		{
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!disposed && disposing)
			{
				this.ForEach(item => item?.Dispose());
			}
		}

		public void Dispose()
		{
			GC.SuppressFinalize(this);
			Dispose(true);
		}
	}
}
