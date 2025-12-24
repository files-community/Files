// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Data.Enums;

namespace Files.App.Data.Contracts
{
	public interface IThumbnailCache
	{
		Task<byte[]?> GetAsync(string path, int size, IconOptions options, CancellationToken ct);

		Task SetAsync(string path, int size, IconOptions options, byte[] thumbnail, CancellationToken ct);

		Task<long> GetSizeAsync();

		Task EvictToSizeAsync(long targetSizeBytes);

		Task ClearAsync();
	}
}
