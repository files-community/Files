// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Contracts
{
	public interface IThumbnailGenerator
	{
		IEnumerable<string> SupportedTypes { get; }

		Task<byte[]?> GenerateAsync(
			string path,
			int size,
			bool isFolder,
			IconOptions options,
			CancellationToken ct);
	}
}
