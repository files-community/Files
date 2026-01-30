// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Contracts
{
	/// <summary>
	/// Generates thumbnails for specific file types.
	/// </summary>
	public interface IThumbnailGenerator
	{
		/// <summary>
		/// Gets the file extensions this generator supports.
		/// </summary>
		IEnumerable<string> SupportedTypes { get; }

		/// <summary>
		/// Generates a thumbnail for the specified path.
		/// </summary>
		/// <returns>Thumbnail bytes, or null if generation fails.</returns>
		Task<byte[]?> GenerateAsync(
			string path,
			int size,
			bool isFolder,
			IconOptions options,
			CancellationToken ct);
	}
}
