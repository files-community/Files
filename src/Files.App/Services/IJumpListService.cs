// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Services
{
	public interface IJumpListService
	{
		Task AddFolderAsync(string path);

		Task RefreshPinnedFoldersAsync();

		Task RemoveFolderAsync(string path);

		Task<IEnumerable<string>> GetFoldersAsync();
	}
}
