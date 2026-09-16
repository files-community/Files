// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using System.Collections.Concurrent;

namespace Files.App.Utils.Storage
{
	/// <inheritdoc cref="IStorageCacheService"/>
	internal sealed class StorageCacheService : IStorageCacheService
	{
		private const int MaxEntries = 10_000;

		private readonly ConcurrentDictionary<string, string> cachedDictionary = new();

		/// <inheritdoc/>
		public ValueTask<string> GetDisplayName(string path, CancellationToken cancellationToken)
		{
			return
				cachedDictionary.TryGetValue(path, out var displayName)
					? ValueTask.FromResult(displayName)
					: ValueTask.FromResult(string.Empty);
		}

		/// <inheritdoc/>
		public ValueTask AddDisplayName(string path, string? displayName)
		{
			if (string.IsNullOrEmpty(displayName))
			{
				cachedDictionary.TryRemove(path, out _);
				return ValueTask.CompletedTask;
			}

			if (cachedDictionary.Count >= MaxEntries && !cachedDictionary.ContainsKey(path))
				return ValueTask.CompletedTask;

			cachedDictionary[path] = displayName;

			return ValueTask.CompletedTask;
		}
	}
}
