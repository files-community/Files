using Files.Backend.Models;
using Files.Shared.Utils;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Files.Backend.AppModels
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
			this.storageSemaphore = new(1, 1);
			this.settingsCache = new();
		}

		/// <inheritdoc/>
		public abstract TValue? GetValue<TValue>(string key, Func<TValue>? defaultValue = null);

		/// <inheritdoc/>
		public abstract bool SetValue<TValue>(string key, TValue? value);

		/// <inheritdoc/>
		public abstract Task<bool> LoadAsync(CancellationToken cancellationToken = default);

		/// <inheritdoc/>
		public abstract Task<bool> SaveAsync(CancellationToken cancellationToken = default);
	}
}
