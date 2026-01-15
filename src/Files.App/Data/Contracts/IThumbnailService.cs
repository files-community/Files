// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Contracts
{
	/// <summary>
	/// Provides thumbnail retrieval and cache management.
	/// </summary>
	public interface IThumbnailService
	{
		/// <summary>
		/// Gets a thumbnail for the specified path.
		/// </summary>
		/// <returns>Thumbnail bytes, or null if unavailable.</returns>
		Task<byte[]?> GetThumbnailAsync(
			string path,
			int size,
			bool isFolder,
			IconOptions options = IconOptions.None,
			CancellationToken ct = default);

		/// <summary>
		/// Registers a thumbnail generator.
		/// </summary>
		void RegisterGenerator(IThumbnailGenerator generator);

		/// <summary>
		/// Clears all cached thumbnails.
		/// </summary>
		Task ClearCacheAsync();

		/// <summary>
		/// Gets the current cache size in bytes.
		/// </summary>
		Task<long> GetCacheSizeAsync();

		/// <summary>
		/// Reduces cache size to the specified target in bytes.
		/// </summary>
		Task EvictCacheAsync(long targetSizeBytes);
	}
}
