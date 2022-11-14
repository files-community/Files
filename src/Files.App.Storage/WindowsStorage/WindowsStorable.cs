using Files.Sdk.Storage;
using Files.Sdk.Storage.LocatableStorage;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace Files.App.Storage.WindowsStorage
{
	/// <inheritdoc cref="IStorable"/>
	public abstract class WindowsStorable<TStorage> : ILocatableStorable
		where TStorage : class, IStorageItem
	{
		internal readonly TStorage storage;

		/// <inheritdoc/>
		public string Path { get; protected internal set; }

		/// <inheritdoc/>
		public string Name { get; protected internal set; }

		/// <inheritdoc/>
		public string Id { get; protected internal set; }

		protected internal WindowsStorable(TStorage storage)
		{
			this.storage = storage;
			this.Path = storage.Path;
			this.Name = storage.Name;
			this.Id = string.Empty;
		}

		/// <inheritdoc/>
		public abstract Task<ILocatableFolder?> GetParentAsync(CancellationToken cancellationToken = default);
	}
}
