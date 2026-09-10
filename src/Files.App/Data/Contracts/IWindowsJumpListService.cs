// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

namespace Files.App.Data.Contracts
{
	public interface IWindowsJumpListService
	{
		Task InitializeAsync();

		Task AddFolderAsync(string path);

		Task RefreshPinnedFoldersAsync();

		Task RemoveFolderAsync(string path);

		/// <summary>
		/// Removes multiple folders using a single Jump List update.
		/// </summary>
		Task RemoveFoldersAsync(IEnumerable<string> paths);

		Task<IEnumerable<string>> GetFoldersAsync();
	}
}
