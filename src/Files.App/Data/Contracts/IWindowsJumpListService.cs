// Copyright (c) 2018-2025 Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Contracts
{
	public interface IWindowsJumpListService
	{
		Task InitializeAsync();

		Task AddFolderAsync(string path);

		Task RefreshPinnedFoldersAsync();

		Task RemoveFolderAsync(string path);

		Task<IEnumerable<string>> GetFoldersAsync();
	}
}
