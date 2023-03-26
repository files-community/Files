﻿using System.Collections.Generic;
using System.Threading.Tasks;

namespace Files.Shared.Services
{
	public interface IJumpListService
	{
		Task AddFolderAsync(string path);
		Task RefreshPinnedFoldersAsync();
		Task RemoveFolderAsync(string path);
		Task<IEnumerable<string>> GetFoldersAsync();
	}
}