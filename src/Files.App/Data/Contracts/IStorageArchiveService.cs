// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Text;
using SevenZip;

namespace Files.App.Data.Contracts
{
	/// <summary>
	/// Represents a service to manage storage archives, powered by 7zip and its C# wrapper SevenZipSharp.
	/// </summary>
	public interface IStorageArchiveService
	{
		/// <summary>
		/// Gets the value that indicates whether specified items can be compressed.
		/// </summary>
		/// <param name="items">Items to check if they can be compressed.</param>
		/// <returns>True if can be compressed; otherwise, false.</returns>
		bool CanCompress(IReadOnlyList<ListedItem> items);

		/// <summary>
		/// Gets the value that indicates whether specified items can be decompressed.
		/// </summary>
		/// <param name="items">Items to check if they can be decompressed.</param>
		/// <returns>True if can be decompressed; otherwise, false.</returns>
		bool CanDecompress(IReadOnlyList<ListedItem> items);

		/// <summary>
		/// Compresses the specified items.
		/// </summary>
		/// <param name="creator">A valid instance of <see cref="ICompressArchiveModel"/>.</param>
		/// <returns>True if the compression has done successfully; otherwise, false.</returns>
		Task<bool> CompressAsync(ICompressArchiveModel compressionModel);

		/// <summary>
		/// Decompresses the archive file specified by the path to the path specified by the path with password if applicable.
		/// </summary>
		/// <param name="archiveFilePath">The archive file path to decompress.</param>
		/// <param name="destinationFolderPath">The destination folder path which the archive file will be decompressed to.</param>
		/// <param name="password">The password to decrypt the archive file if applicable.</param>
		/// <param name="encoding">The file name encoding to decrypt the archive file. If set to null, system default encoding will be used.</param>
		/// <returns>True if the decompression has done successfully; otherwise, false.</returns>
		Task<bool> DecompressAsync(string archiveFilePath, string destinationFolderPath, string password = "", Encoding? encoding = null);

		/// <summary>
		/// Generates the archive file name from item names.
		/// </summary>
		/// <param name="items">Item names to generate archive file name.</param>
		/// <returns></returns>
		string GenerateArchiveNameFromItems(IReadOnlyList<ListedItem> items);

		/// <summary>
		/// Gets the value that indicates whether the archive file is encrypted.
		/// </summary>
		/// <param name="archiveFilePath">The archive file path to check if the item is encrypted.</param>
		/// <returns>True if the archive file is encrypted; otherwise, false.</returns>
		Task<bool> IsEncryptedAsync(string archiveFilePath);

		/// <summary>
		/// Gets the value that indicates whether the archive file's encoding is undetermined.
		/// </summary>
		/// <param name="archiveFilePath">The archive file path to check if the encoding is undetermined.</param>
		/// <returns>True if the archive file's encoding is undetermined; otherwise, false.</returns>
		Task<bool> IsEncodingUndeterminedAsync(string archiveFilePath);

		/// <summary>
		/// Detect encoding for a zip file whose encoding is undetermined.
		/// </summary>
		/// <param name="archiveFilePath">The archive file path to detect encoding</param>
		/// <returns>Null if the archive file doesn't need to detect encoding or its encoding can't be detected; otherwise, the encoding detected.</returns>
		Task<Encoding?> DetectEncodingAsync(string archiveFilePath);

		/// <summary>
		/// Gets the <see cref="SevenZipExtractor"/> instance from the archive file path.
		/// </summary>
		/// <param name="archiveFilePath">The archive file path to generate an instance.</param>
		/// <param name="password">The password to decrypt the archive file if applicable.</param>
		/// <returns>An instance of <see cref="SevenZipExtractor"/> if the specified item is archive; otherwise null.</returns>
		Task<SevenZipExtractor?> GetSevenZipExtractorAsync(string archiveFilePath, string password = "");
	}
}
