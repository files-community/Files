// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Collections.Concurrent;

namespace Files.App.Helpers.FileListCache
{
	internal class FileListCacheController : IFileListCache
	{
		private static FileListCacheController instance;

		public static FileListCacheController GetInstance()
		{
			return instance ??= new FileListCacheController();
		}

		private FileListCacheController()
		{
		}

		private readonly ConcurrentDictionary<string, string> fileNamesCache = new();

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