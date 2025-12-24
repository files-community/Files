// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Contracts
{
	public interface IThumbnailService
	{
		Task<byte[]?> GetThumbnailAsync(
			string path,
			int size,
			bool isFolder,
			IconOptions options = IconOptions.None,
			CancellationToken ct = default);

		void RegisterGenerator(IThumbnailGenerator generator);

		Task ClearCacheAsync();

		Task<long> GetCacheSizeAsync();

		Task EvictCacheAsync(long targetSizeBytes);
	}
}
