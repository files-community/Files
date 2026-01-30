// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Contracts
{
	/// <summary>
	/// Stores and retrieves cached thumbnails.
	/// </summary>
	public interface IThumbnailCache
	{
		/// <summary>
		/// Retrieves a cached thumbnail.
		/// </summary>
		/// <returns>Thumbnail bytes, or null if not cached.</returns>
		Task<byte[]?> GetAsync(string path, int size, IconOptions options, CancellationToken ct);

		/// <summary>
		/// Stores a thumbnail in the cache.
		/// </summary>
		Task SetAsync(string path, int size, IconOptions options, byte[] thumbnail, CancellationToken ct);

		/// <summary>
		/// Gets the current cache size in bytes.
		/// </summary>
		Task<long> GetSizeAsync();

		/// <summary>
		/// Reduces cache size to the specified target in bytes.
		/// </summary>
		Task EvictToSizeAsync(long targetSizeBytes);

		/// <summary>
		/// Removes all cached thumbnails.
		/// </summary>
		Task ClearAsync();

		/// <summary>
		/// Retrieves a cached icon from the in-memory icon cache.
		/// </summary>
		/// <returns>Icon bytes, or null if not cached.</returns>
		byte[]? GetIcon(string extension, int size);

		/// <summary>
		/// Stores an icon in the in-memory icon cache.
		/// </summary>
		void SetIcon(string extension, int size, byte[] iconData);
	}
}
