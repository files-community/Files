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
		/// <inheritdoc/>
		public string Id => string.Empty;

		/// <inheritdoc/>
		public string Name => Storage.Name;

		/// <inheritdoc/>
		public string Path => Storage.Path;

		internal TStorage Storage { get; }

		protected internal WindowsStorable(TStorage storage) => Storage = storage;

		/// <inheritdoc/>
		public abstract Task<ILocatableFolder?> GetParentAsync(CancellationToken cancellationToken = default);
	}
}
