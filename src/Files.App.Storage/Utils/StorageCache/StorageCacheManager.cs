// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Collections.Concurrent;

namespace Files.App.Storage
{
	/// <inheritdoc cref="IStorageCacheManager"/>
	public class StorageCacheManager : IStorageCacheManager
	{
		// Fields

		private static StorageCacheManager? _instance;
		private readonly ConcurrentDictionary<string, string> _storageCacheList = new();

		// Constructor

		private StorageCacheManager() { }

		// Methods

		/// <inheritdoc/>
		public ValueTask<string> GetFileNameFromCache(string path, CancellationToken cancellationToken)
		{
			return
				_storageCacheList.TryGetValue(path, out var displayName)
					? ValueTask.FromResult(displayName)
					: ValueTask.FromResult(string.Empty);
		}

		/// <inheritdoc/>
		public ValueTask SaveFileNameToCache(string path, string displayName)
		{
			if (string.IsNullOrEmpty(displayName))
				_storageCacheList.TryRemove(path, out _);

			_storageCacheList[path] = displayName;

			return ValueTask.CompletedTask;
		}

		/// <summary>
		/// Initializes single instance of <see cref="StorageCacheManager"/>.
		/// </summary>
		public static StorageCacheManager InitializeInstance()
		{
			return _instance ??= new();
		}
	}
}
