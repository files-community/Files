using Files.Shared.Utils;
using System.Collections.Concurrent;
using System.IO;

namespace Files.Core.Data.AppModels
{
	/// <summary>
	/// Represents a dictionary-based database model.
	/// </summary>
	public abstract class BaseDatabaseModel<TDictionaryValue> : IDatabaseModel<string>
	{
		protected readonly IAsyncSerializer<Stream> serializer;
		protected readonly SemaphoreSlim storageSemaphore;
		protected readonly ConcurrentDictionary<string, TDictionaryValue> settingsCache;

		protected BaseDatabaseModel(IAsyncSerializer<Stream> serializer)
		{
			this.serializer = serializer;
			storageSemaphore = new(1, 1);
			settingsCache = new();
		}

		/// <inheritdoc/>
		public abstract TValue? GetValue<TValue>(string key, Func<TValue>? defaultValue = null);

		/// <inheritdoc/>
		public abstract bool SetValue<TValue>(string key, TValue? value);

		/// <inheritdoc/>
		public abstract Task LoadAsync(CancellationToken cancellationToken = default);

		/// <inheritdoc/>
		public abstract Task SaveAsync(CancellationToken cancellationToken = default);

		/// <inheritdoc/>
		public virtual void Dispose()
		{
			storageSemaphore.Dispose();
			settingsCache.Clear();
		}
	}
}
