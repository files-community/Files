﻿using Files.Enums;
using Files.Filesystem;
using Microsoft.Toolkit.Uwp.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Storage;

namespace Files.Helpers
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
                SortOption.OriginalPath => item => (item as RecycleBinItem)?.ItemOriginalFolder,
                SortOption.DateDeleted => item => (item as RecycleBinItem)?.ItemDateDeletedReal,
                _ => null,
            };
        }

        public static IEnumerable<ListedItem> OrderFileList(List<ListedItem> filesAndFolders, SortOption directorySortOption, SortDirection directorySortDirection)
        {
            var orderFunc = GetSortFunc(directorySortOption);
            var naturalStringComparer = NaturalStringComparer.GetForProcessor();

            // In ascending order, show folders first, then files.
            // So, we use == StorageItemTypes.File to make the value for a folder equal to 0, and equal to 1 for the rest.
            static bool folderThenFileAsync(ListedItem listedItem) => (listedItem.PrimaryItemAttribute == StorageItemTypes.File);
            IOrderedEnumerable<ListedItem> ordered;

            if (directorySortDirection == SortDirection.Ascending)
            {
                if (directorySortOption == SortOption.Name)
                {
                    if (App.AppSettings.ListAndSortDirectoriesAlongsideFiles)
                    {
                        ordered = filesAndFolders.OrderBy(orderFunc, naturalStringComparer);
                    }
                    else
                    {
                        ordered = filesAndFolders.OrderBy(folderThenFileAsync).ThenBy(orderFunc, naturalStringComparer);
                    }
                }
                else
                {
                    if (App.AppSettings.ListAndSortDirectoriesAlongsideFiles)
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
                    if (App.AppSettings.ListAndSortDirectoriesAlongsideFiles)
                    {
                        ordered = filesAndFolders.OrderByDescending(orderFunc, naturalStringComparer);
                    }
                    else
                    {
                        ordered = filesAndFolders.OrderBy(folderThenFileAsync).ThenByDescending(orderFunc, naturalStringComparer);
                    }
                }
                else
                {
                    if (App.AppSettings.ListAndSortDirectoriesAlongsideFiles)
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