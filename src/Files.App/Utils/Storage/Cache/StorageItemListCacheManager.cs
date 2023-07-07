// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Collections.Concurrent;

namespace Files.App.Utils.Storage
{
	internal class StorageItemListCacheManager : IStorageItemListCacheManager
	{
		private static StorageItemListCacheManager instance;

		public static StorageItemListCacheManager GetInstance()
		{
			return instance ??= new StorageItemListCacheManager();
		}

		private readonly ConcurrentDictionary<string, string> fileNamesCache = new();

		private StorageItemListCacheManager()
		{
		}

		public ValueTask<string> ReadFileDisplayNameFromCache(string path, CancellationToken cancellationToken)
		{
			return fileNamesCache.TryGetValue(path, out var displayName) ? ValueTask.FromResult(displayName) : ValueTask.FromResult(string.Empty);
		}

		public ValueTask SaveFileDisplayNameToCache(string path, string displayName)
		{
			if (displayName is null)
			{
				fileNamesCache.TryRemove(path, out _);
			}

			fileNamesCache[path] = displayName;
			return ValueTask.CompletedTask;
		}
	}
}
