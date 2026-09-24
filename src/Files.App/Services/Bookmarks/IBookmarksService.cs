$\xEF\xBB\xBF// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Services
{
	public interface IBookmarksService
	{
		Task<List<BookmarkItem>> LoadAsync();

		Task SaveAsync(IEnumerable<BookmarkItem> items);
	}
}
