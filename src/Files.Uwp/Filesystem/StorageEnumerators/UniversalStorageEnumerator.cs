using Files.Uwp.Extensions;
using Files.Uwp.Filesystem.StorageItems;
using Files.Uwp.Helpers;
using Files.Uwp.Helpers.ListedItem;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;

namespace Files.Uwp.Filesystem.StorageEnumerators
{
    public static class UniversalStorageEnumerator
    {
        public static async Task<List<ListedItem>> ListEntries(
            BaseStorageFolder rootFolder,
            StorageFolderWithPath currentStorageFolder,
            string returnformat,
            Type sourcePageType,
            CancellationTokenSource cancellationTokenSource,
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
                        var folder = await ListedItemHelpers.AddFolderAsync(item.AsBaseStorageFolder(), currentStorageFolder, returnformat, cancellationTokenSource);

                    }
                    else
                    {
                        var fileEntry = await ListedItemHelpers.AddFileAsync(item.AsBaseStorageFile(), currentStorageFolder, returnformat, cancellationTokenSource);
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
                    if (cancellationTokenSource.Token.IsCancellationRequested)
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
    }
}
