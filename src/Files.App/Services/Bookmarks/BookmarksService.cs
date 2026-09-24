$\xEF\xBB\xBF// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Windows.Storage;

namespace Files.App.Services
{
	internal sealed class BookmarksService : IBookmarksService
	{
		private static string FilePath
			=> SystemIO.Path.Combine(ApplicationData.Current.LocalFolder.Path, "bookmarks.json");

		public async Task<List<BookmarkItem>> LoadAsync()
		{
			try
			{
				if (!SystemIO.File.Exists(FilePath))
					return [];

				await using var stream = SystemIO.File.OpenRead(FilePath);
				return await JsonSerializer.DeserializeAsync(stream, AppJsonSerializerContext.Default.ListBookmarkItem) ?? [];
			}
			catch (Exception ex)
			{
				App.Logger.LogWarning(ex, "Failed to load the bookmarks.");
				return [];
			}
		}

		public async Task SaveAsync(IEnumerable<BookmarkItem> items)
		{
			try
			{
				await using var stream = SystemIO.File.Create(FilePath);
				await JsonSerializer.SerializeAsync(stream, items.ToList(), AppJsonSerializerContext.Default.ListBookmarkItem);
			}
			catch (Exception ex)
			{
				App.Logger.LogWarning(ex, "Failed to save the bookmarks.");
			}
		}
	}
}
