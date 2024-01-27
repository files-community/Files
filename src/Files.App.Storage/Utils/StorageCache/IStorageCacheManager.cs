// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Storage
{
	/// <summary>
	/// Provides manager for storage cache.
	/// </summary>
	public interface IStorageCacheManager
	{
		/// <summary>
		/// Gets file display name from cache.
		/// </summary>
		public ValueTask<string> GetFileNameFromCache(string path, CancellationToken cancellationToken);

		/// <summary>
		/// Sets file display name to cache.
		/// </summary>
		public ValueTask SetFileNameToCache(string path, string displayName);
	}
}
