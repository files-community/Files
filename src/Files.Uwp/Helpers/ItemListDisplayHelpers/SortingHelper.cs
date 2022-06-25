using Files.Shared.Enums;
using Files.Uwp.Filesystem;
using CommunityToolkit.Mvvm.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Storage;

namespace Files.Uwp.Helpers
{
    public static class SortingHelper
    {
        private static object orderByNameFunc(ListedItem item) => item.ItemName;

        public static Func<ListedItem, object> GetSortFunc(SortOption directorySortOption)
        {
            return directorySortOption switch
            {
                SortOption.Name => item => item.ItemName,
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
            static bool folderThenFileAsync(ListedItem listedItem) => (listedItem.PrimaryItemAttribute == StorageItemTypes.File || listedItem.IsShortcutItem || listedItem.IsZipItem);
            IOrderedEnumerable<ListedItem> ordered;

            if (directorySortDirection == SortDirection.Ascending)
            {
                if (directorySortOption == SortOption.Name)
                {
                    if (sortDirectoriesAlongsideFiles)
                    {
                        ordered = filesAndFolders.OrderBy(orderFunc, naturalStringComparer);
                    }
                    else
                    {
                        ordered = filesAndFolders.OrderBy(folderThenFileAsync).ThenBy(orderFunc, naturalStringComparer);
                    }
                }
                else if (directorySortOption == SortOption.FileTag)
                {
                    if (sortDirectoriesAlongsideFiles)
                    {
                        ordered = filesAndFolders.OrderBy(x => string.IsNullOrEmpty(orderFunc(x) as string)).ThenBy(orderFunc);
                    }
                    else
                    {
                        ordered = filesAndFolders.OrderBy(folderThenFileAsync).ThenBy(x => string.IsNullOrEmpty(orderFunc(x) as string)).ThenBy(orderFunc);
                    }
                }
                else
                {
                    if (sortDirectoriesAlongsideFiles)
                    {
                        ordered = filesAndFolders.OrderBy(orderFunc);
                    }
                    else
                    {
                        ordered = filesAndFolders.OrderBy(folderThenFileAsync).ThenBy(orderFunc);
                    }
                }
            }
            else
            {
                if (directorySortOption == SortOption.Name)
                {
                    if (sortDirectoriesAlongsideFiles)
                    {
                        ordered = filesAndFolders.OrderByDescending(orderFunc, naturalStringComparer);
                    }
                    else
                    {
                        ordered = filesAndFolders.OrderBy(folderThenFileAsync).ThenByDescending(orderFunc, naturalStringComparer);
                    }
                }
                else if (directorySortOption == SortOption.FileTag)
                {
                    if (sortDirectoriesAlongsideFiles)
                    {
                        ordered = filesAndFolders.OrderBy(x => string.IsNullOrEmpty(orderFunc(x) as string)).ThenByDescending(orderFunc);
                    }
                    else
                    {
                        ordered = filesAndFolders.OrderBy(folderThenFileAsync).ThenBy(x => string.IsNullOrEmpty(orderFunc(x) as string)).ThenByDescending(orderFunc);
                    }
                }
                else
                {
                    if (sortDirectoriesAlongsideFiles)
                    {
                        ordered = filesAndFolders.OrderByDescending(orderFunc);
                    }
                    else
                    {
                        ordered = filesAndFolders.OrderBy(folderThenFileAsync).ThenByDescending(orderFunc);
                    }
                }
            }

            // Further order by name if applicable
            if (directorySortOption != SortOption.Name)
            {
                if (directorySortDirection == SortDirection.Ascending)
                {
                    ordered = ordered.ThenBy(orderByNameFunc, naturalStringComparer);
                }
                else
                {
                    ordered = ordered.ThenByDescending(orderByNameFunc, naturalStringComparer);
                }
            }

            return ordered;
        }
    }
}