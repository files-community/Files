using ByteSizeLib;
using Files.Extensions;
using Files.Filesystem.StorageItems;
using Files.Helpers;
using Files.Services;
using Files.Views.LayoutModes;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;

namespace Files.Filesystem.StorageEnumerators
{
    public static class UniversalStorageEnumerator
    {
        public static async Task<List<ListedItem>> ListEntries(
            BaseStorageFolder rootFolder,
            StorageFolderWithPath currentStorageFolder,
            string returnformat,
            Type sourcePageType,
            CancellationToken cancellationToken,
            int countLimit,
            Func<List<ListedItem>, Task> intermediateAction,
            Dictionary<string, BitmapImage> defaultIconPairs = null
        )
        {
            var sampler = new IntervalSampler(500);
            var tempList = new List<ListedItem>();
            uint count = 0;
            var firstRound = true;
            while (true)
            {
                IReadOnlyList<IStorageItem> items;
                uint maxItemsToRetrieve = 300;

                if (intermediateAction == null)
                {
                    // without intermediate action increase batches significantly
                    maxItemsToRetrieve = 1000;
                }
                else if (firstRound)
                {
                    maxItemsToRetrieve = 32;
                    firstRound = false;
                }
                try
                {
                    items = await rootFolder.GetItemsAsync(count, maxItemsToRetrieve);
                    if (items == null || items.Count == 0)
                    {
                        break;
                    }
                }
                catch (NotImplementedException)
                {
                    break;
                }
                catch (Exception ex) when (
                    ex is UnauthorizedAccessException
                    || ex is FileNotFoundException
                    || (uint)ex.HResult == 0x80070490) // ERROR_NOT_FOUND
                {
                    // If some unexpected exception is thrown - enumerate this folder file by file - just to be sure
                    items = await EnumerateFileByFile(rootFolder, count, maxItemsToRetrieve);
                }
                foreach (var item in items)
                {
                    if (item.IsOfType(StorageItemTypes.Folder))
                    {
                        var folder = await AddFolderAsync(item.AsBaseStorageFolder(), currentStorageFolder, returnformat, cancellationToken);
                        if (folder != null)
                        {
                            if (defaultIconPairs?.ContainsKey(string.Empty) ?? false)
                            {
                                folder.SetDefaultIcon(defaultIconPairs[string.Empty]);
                            }
                            tempList.Add(folder);
                        }
                    }
                    else
                    {
                        var fileEntry = await AddFileAsync(item.AsBaseStorageFile(), currentStorageFolder, returnformat, cancellationToken);
                        if (fileEntry != null)
                        {
                            if (defaultIconPairs != null)
                            {
                                if (!string.IsNullOrEmpty(fileEntry.FileExtension))
                                {
                                    var lowercaseExtension = fileEntry.FileExtension.ToLowerInvariant();
                                    if (defaultIconPairs.ContainsKey(lowercaseExtension))
                                    {
                                        fileEntry.SetDefaultIcon(defaultIconPairs[lowercaseExtension]);
                                    }
                                }
                            }
                            tempList.Add(fileEntry);
                        }
                    }
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                }
                count += maxItemsToRetrieve;

                if (countLimit > -1 && count >= countLimit)
                {
                    break;
                }

                if (intermediateAction != null && (items.Count == maxItemsToRetrieve || sampler.CheckNow()))
                {
                    await intermediateAction(tempList);
                    // clear the temporary list every time we do an intermediate action
                    tempList.Clear();
                }
            }
            return tempList;
        }

        private static async Task<IReadOnlyList<IStorageItem>> EnumerateFileByFile(BaseStorageFolder rootFolder, uint startFrom, uint itemsToIterate)
        {
            var tempList = new List<IStorageItem>();
            for (var i = startFrom; i < startFrom + itemsToIterate; i++)
            {
                IStorageItem item;
                try
                {
                    var results = await rootFolder.GetItemsAsync(i, 1);
                    item = results?.FirstOrDefault();
                    if (item == null)
                    {
                        break;
                    }
                }
                catch (NotImplementedException)
                {
                    break;
                }
                catch (Exception ex) when (
                    ex is UnauthorizedAccessException
                    || ex is FileNotFoundException
                    || (uint)ex.HResult == 0x80070490) // ERROR_NOT_FOUND
                {
                    continue;
                }
                tempList.Add(item);
            }
            return tempList;
        }

        private static async Task<ListedItem> AddFolderAsync(BaseStorageFolder folder, StorageFolderWithPath currentStorageFolder, string dateReturnFormat, CancellationToken cancellationToken)
        {
            var basicProperties = await folder.GetBasicPropertiesAsync();
            if (!cancellationToken.IsCancellationRequested)
            {
                return new ListedItem(folder.FolderRelativeId, dateReturnFormat)
                {
                    PrimaryItemAttribute = StorageItemTypes.Folder,
                    ItemName = folder.DisplayName,
                    ItemDateModifiedReal = basicProperties.DateModified,
                    ItemDateCreatedReal = folder.DateCreated,
                    ItemType = folder.DisplayType,
                    IsHiddenItem = false,
                    Opacity = 1,
                    FileImage = null,
                    LoadFileIcon = false,
                    ItemPath = string.IsNullOrEmpty(folder.Path) ? PathNormalization.Combine(currentStorageFolder.Path, folder.Name) : folder.Path,
                    FileSize = null,
                    FileSizeBytes = 0
                };
            }
            return null;
        }

        private static async Task<ListedItem> AddFileAsync(
            BaseStorageFile file,
            StorageFolderWithPath currentStorageFolder,
            string dateReturnFormat,
            CancellationToken cancellationToken
        )
        {
            IUserSettingsService userSettingsService = Ioc.Default.GetService<IUserSettingsService>();

            var basicProperties = await file.GetBasicPropertiesAsync();
            // Display name does not include extension
            var itemName = string.IsNullOrEmpty(file.DisplayName) || userSettingsService.PreferencesSettingsService.ShowFileExtensions ?
                file.Name : file.DisplayName;
            var itemModifiedDate = basicProperties.DateModified;
            var itemCreatedDate = file.DateCreated;
            var itemPath = string.IsNullOrEmpty(file.Path) ? PathNormalization.Combine(currentStorageFolder.Path, file.Name) : file.Path;
            var itemSize = ByteSize.FromBytes(basicProperties.Size).ToBinaryString().ConvertSizeAbbreviation();
            var itemSizeBytes = basicProperties.Size;
            var itemType = file.DisplayType;
            var itemFileExtension = file.FileType;
            var itemThumbnailImgVis = false;

            if (cancellationToken.IsCancellationRequested)
            {
                return null;
            }

            if (file.Name.EndsWith(".lnk") || file.Name.EndsWith(".url"))
            {
                // This shouldn't happen, StorageFile api does not support shortcuts
                Debug.WriteLine("Something strange: StorageFile api returned a shortcut");
            }
            // TODO: is this needed to be handled here?
            else if (App.LibraryManager.TryGetLibrary(file.Path, out LibraryLocationItem library))
            {
                return new LibraryItem(library)
                {
                    ItemDateModifiedReal = itemModifiedDate,
                    ItemDateCreatedReal = itemCreatedDate,
                };
            }
            else
            {
                return new ListedItem(file.FolderRelativeId, dateReturnFormat)
                {
                    PrimaryItemAttribute = StorageItemTypes.File,
                    FileExtension = itemFileExtension,
                    IsHiddenItem = false,
                    Opacity = 1,
                    FileImage = null,
                    LoadFileIcon = itemThumbnailImgVis,
                    ItemName = itemName,
                    ItemDateModifiedReal = itemModifiedDate,
                    ItemDateCreatedReal = itemCreatedDate,
                    ItemType = itemType,
                    ItemPath = itemPath,
                    FileSize = itemSize,
                    FileSizeBytes = (long)itemSizeBytes,
                };
            }
            return null;
        }
    }
}