// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Utils.Storage
{
	/// <summary>
	/// Represents manager for storage item caching.
	/// </summary>
	internal interface IStorageCacheController
	{
		/// <summary>
		/// Get storage items from caching pool.
		/// </summary>
		/// <param name="path">Source path to restore.</param>
		/// <param name="cancellationToken">Token to cancel task.</param>
		/// <returns></returns>
		public ValueTask<string> ReadFileDisplayNameFromCache(string path, CancellationToken cancellationToken);

		/// <summary>
		/// Set storage items to caching pool.
		/// </summary>
		/// <param name="path">Source path to cache.</param>
		/// <param name="displayName">Display name of cache.</param>
		/// <returns></returns>
		public ValueTask SaveFileDisplayNameToCache(string path, string displayName);
	}
}
