// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading.Tasks;
using Files.App.Data.Models;

namespace Files.App.Services
{
	public interface IVisitedFoldersService
	{
		void TrackFolder(string folderPath, string? folderName = null);
		IReadOnlyList<VisitedFolderItem> GetTopFolders(int limit = 15);
		IReadOnlyList<VisitedFolderItem> Search(string? query, int limit = 15);
		Task InitializeAsync();
		Task SaveChangesAsync();
	}
}
