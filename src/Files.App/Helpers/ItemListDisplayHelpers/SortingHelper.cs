// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Windows.Storage;

namespace Files.App.Helpers
{
	public static class SortingHelper
	{
		private static object OrderByNameFunc(ListedItem item)
			=> item.Name;

		public static Func<ListedItem, object>? GetSortFunc(SortOption directorySortOption)
		{
			return directorySortOption switch
			{
				SortOption.Name => item => item.Name,
				SortOption.DateModified => item => item.ItemDateModifiedReal,
				SortOption.DateCreated => item => item.ItemDateCreatedReal,
				SortOption.FileType => item => item.ItemType,
				SortOption.Size => item => item.FileSizeBytes,
				SortOption.SyncStatus => item => item.SyncStatusString,
				SortOption.FileTag => item => item.FileTags?.FirstOrDefault(),
				SortOption.OriginalFolder => item => (item as RecycleBinItem)?.ItemOriginalFolder,
				SortOption.DateDeleted => item => (item as RecycleBinItem)?.ItemDateDeletedReal,
				_ => null,
			};
		}

		public static IEnumerable<ListedItem> OrderFileList(List<ListedItem> filesAndFolders, SortOption directorySortOption, SortDirection directorySortDirection, bool sortDirectoriesAlongsideFiles)
		{
			var orderFunc = GetSortFunc(directorySortOption);
			var naturalStringComparer = NaturalStringComparer.GetForProcessor();

			// In ascending order, show folders first, then files.
			// So, we use == StorageItemTypes.File to make the value for a folder equal to 0, and equal to 1 for the rest.
			static bool FolderThenFileAsync(ListedItem listedItem)
				=> (listedItem.PrimaryItemAttribute == StorageItemTypes.File || listedItem.IsShortcut || listedItem.IsArchive);

			IOrderedEnumerable<ListedItem> ordered;

			if (directorySortDirection == SortDirection.Ascending)
			{
				ordered = directorySortOption switch
				{
					SortOption.Name => sortDirectoriesAlongsideFiles
						? filesAndFolders.OrderBy(orderFunc, naturalStringComparer)
						: filesAndFolders.OrderBy(FolderThenFileAsync).ThenBy(orderFunc, naturalStringComparer),

					SortOption.FileTag => sortDirectoriesAlongsideFiles
						? filesAndFolders.OrderBy(x => string.IsNullOrEmpty(orderFunc(x) as string)).ThenBy(orderFunc)
						: filesAndFolders.OrderBy(FolderThenFileAsync)
							.ThenBy(x => string.IsNullOrEmpty(orderFunc(x) as string))
							.ThenBy(orderFunc),

					_ => sortDirectoriesAlongsideFiles
						? filesAndFolders.OrderBy(orderFunc)
						: filesAndFolders.OrderBy(FolderThenFileAsync).ThenBy(orderFunc)
				};
			}
			else
			{
				ordered = directorySortOption switch
				{
					SortOption.Name => sortDirectoriesAlongsideFiles
						? filesAndFolders.OrderByDescending(orderFunc, naturalStringComparer)
						: filesAndFolders.OrderBy(FolderThenFileAsync)
							.ThenByDescending(orderFunc, naturalStringComparer),

					SortOption.FileTag => sortDirectoriesAlongsideFiles
						? filesAndFolders.OrderBy(x => string.IsNullOrEmpty(orderFunc(x) as string))
							.ThenByDescending(orderFunc)
						: filesAndFolders.OrderBy(FolderThenFileAsync)
							.ThenBy(x => string.IsNullOrEmpty(orderFunc(x) as string))
							.ThenByDescending(orderFunc),

					_ => sortDirectoriesAlongsideFiles
						? filesAndFolders.OrderByDescending(orderFunc)
						: filesAndFolders.OrderBy(FolderThenFileAsync).ThenByDescending(orderFunc)
				};
			}

			// Further order by name if applicable
			if (directorySortOption != SortOption.Name)
			{
				ordered = directorySortDirection == SortDirection.Ascending
					? ordered.ThenBy(OrderByNameFunc, naturalStringComparer)
					: ordered.ThenByDescending(OrderByNameFunc, naturalStringComparer);
			}

			return ordered;
		}
	}
}