// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using SevenZip;

namespace Files.App.Data.Contracts
{
	public interface IStorageArchiveService
	{
		bool CanCompress(IReadOnlyList<ListedItem> items);

		bool CanDecompress(IReadOnlyList<ListedItem> items);

		Task CompressAsync(ICompressArchiveModel creator);

		Task<bool> DecompressAsync(BaseStorageFile archive, BaseStorageFolder destinationFolder, string password);

		string GenerateArchiveNameFromItems(IReadOnlyList<ListedItem> items);

		Task<bool> IsEncryptedAsync(BaseStorageFile archive);

		Task<SevenZipExtractor?> GetSevenZipExtractorAsync(BaseStorageFile archive, string password = "");
	}
}
