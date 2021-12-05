using ByteSizeLib;
using Files.Common;
using Files.Extensions;
using Files.Filesystem.StorageItems;
using Files.Helpers;
using Files.Helpers.FileListCache;
using Files.Services;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using Microsoft.Toolkit.Uwp;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;
using static Files.Helpers.NativeFindStorageItemHelper;
using FileAttributes = System.IO.FileAttributes;

namespace Files.Filesystem.StorageEnumerators
{
    public static class Win32StorageEnumerator
    {
        private static string folderTypeTextLocalized = "FileFolderListItem".GetLocalized();
        private static IFileListCache fileListCache = FileListCacheController.GetInstance();

        public static async Task<List<ListedItem>> ListEntries(
            string path,
            string returnformat,
            IntPtr hFile,
            WIN32_FIND_DATA findData,
            NamedPipeAsAppServiceConnection connection,
            CancellationToken cancellationToken,
            int countLimit,
            Func<List<ListedItem>, Task> intermediateAction,
            Dictionary<string, BitmapImage> defaultIconPairs = null
        )
        {
            var sampler = new IntervalSampler(500);
            var tempList = new List<ListedItem>();
            var hasNextFile = false;
            var count = 0;

            IUserSettingsService userSettingsService = Ioc.Default.GetService<IUserSettingsService>();
            bool showFolderSize = userSettingsService.PreferencesSettingsService.ShowFolderSize;

            do
            {
                var isSystem = ((FileAttributes)findData.dwFileAttributes & FileAttributes.System) == FileAttributes.System;
                var isHidden = ((FileAttributes)findData.dwFileAttributes & FileAttributes.Hidden) == FileAttributes.Hidden;
                if (!isHidden || (userSettingsService.PreferencesSettingsService.AreHiddenItemsVisible && (!isSystem || !userSettingsService.PreferencesSettingsService.AreSystemItemsHidden)))
                {
                    if (((FileAttributes)findData.dwFileAttributes & FileAttributes.Directory) != FileAttributes.Directory)
                    {
                        var file = await GetFile(findData, path, returnformat, connection, cancellationToken);
                        if (file != null)
                        {
                            if (defaultIconPairs != null)
                            {
                                if (!string.IsNullOrEmpty(file.FileExtension))
                                {
                                    var lowercaseExtension = file.FileExtension.ToLowerInvariant();
                                    if (defaultIconPairs.ContainsKey(lowercaseExtension))
                                    {
                                        file.SetDefaultIcon(defaultIconPairs[lowercaseExtension]);
                                    }
                                }
                            }
                            tempList.Add(file);
                            ++count;
                        }
                    }
                    else if (((FileAttributes)findData.dwFileAttributes & FileAttributes.Directory) == FileAttributes.Directory)
                    {
                        if (findData.cFileName != "." && findData.cFileName != "..")
                        {
                            var folder = await GetFolder(findData, path, returnformat, cancellationToken);
                            if (folder != null)
                            {
                                if (defaultIconPairs?.ContainsKey(string.Empty) ?? false)
                                {
                                    // Set folder icon (found by empty extension string)
                                    folder.SetDefaultIcon(defaultIconPairs[string.Empty]);
                                }
                                tempList.Add(folder);
                                ++count;

                                if (showFolderSize)
                                {
                                    FolderHelpers.UpdateFolder(folder, cancellationToken);
                                }
                            }
                        }
                    }
                }
                if (cancellationToken.IsCancellationRequested || count == countLimit)
                {
                    break;
                }

                hasNextFile = FindNextFile(hFile, out findData);
                if (intermediateAction != null && (count == 32 || sampler.CheckNow()))
                {
                    await intermediateAction(tempList);
                    // clear the temporary list every time we do an intermediate action
                    tempList.Clear();
                }
            } while (hasNextFile);

            FindClose(hFile);
            return tempList;
        }

        public static async Task<ListedItem> GetFolder(
            WIN32_FIND_DATA findData,
            string pathRoot,
            string dateReturnFormat,
            CancellationToken cancellationToken
        )
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return null;
            }

            DateTime itemModifiedDate;
            DateTime itemCreatedDate;
            try
            {
                FileTimeToSystemTime(ref findData.ftLastWriteTime, out SYSTEMTIME systemModifiedTimeOutput);
                itemModifiedDate = systemModifiedTimeOutput.ToDateTime();

                FileTimeToSystemTime(ref findData.ftCreationTime, out SYSTEMTIME systemCreatedTimeOutput);
                itemCreatedDate = systemCreatedTimeOutput.ToDateTime();
            }
            catch (ArgumentException)
            {
                // Invalid date means invalid findData, do not add to list
                return null;
            }
            var itemPath = Path.Combine(pathRoot, findData.cFileName);
            string itemName = await fileListCache.ReadFileDisplayNameFromCache(itemPath, cancellationToken);
            if (string.IsNullOrEmpty(itemName))
            {
                itemName = findData.cFileName;
            }
            bool isHidden = (((FileAttributes)findData.dwFileAttributes & FileAttributes.Hidden) == FileAttributes.Hidden);
            double opacity = 1;

            if (isHidden)
            {
                opacity = Constants.UI.DimItemOpacity;
            }

            return new ListedItem(null, dateReturnFormat)
            {
                PrimaryItemAttribute = StorageItemTypes.Folder,
                ItemNameRaw = itemName,
                ItemDateModifiedReal = itemModifiedDate,
                ItemDateCreatedReal = itemCreatedDate,
                ItemType = folderTypeTextLocalized,
                FileImage = null,
                IsHiddenItem = isHidden,
                Opacity = opacity,
                LoadFileIcon = false,
                ItemPath = itemPath,
                FileSize = null,
                FileSizeBytes = 0,
            };
        }

        public static async Task<ListedItem> GetFile(
            WIN32_FIND_DATA findData,
            string pathRoot,
            string dateReturnFormat,
            NamedPipeAsAppServiceConnection connection,
            CancellationToken cancellationToken
        )
        {
            var itemPath = Path.Combine(pathRoot, findData.cFileName);
            var itemName = findData.cFileName;

            DateTime itemModifiedDate, itemCreatedDate, itemLastAccessDate;
            try
            {
                FileTimeToSystemTime(ref findData.ftLastWriteTime, out SYSTEMTIME systemModifiedDateOutput);
                itemModifiedDate = systemModifiedDateOutput.ToDateTime();

                FileTimeToSystemTime(ref findData.ftCreationTime, out SYSTEMTIME systemCreatedDateOutput);
                itemCreatedDate = systemCreatedDateOutput.ToDateTime();

                FileTimeToSystemTime(ref findData.ftLastAccessTime, out SYSTEMTIME systemLastAccessOutput);
                itemLastAccessDate = systemLastAccessOutput.ToDateTime();
            }
            catch (ArgumentException)
            {
                // Invalid date means invalid findData, do not add to list
                return null;
            }

            long itemSizeBytes = findData.GetSize();
            var itemSize = ByteSize.FromBytes(itemSizeBytes).ToBinaryString().ConvertSizeAbbreviation();
            string itemType = "ItemTypeFile".GetLocalized();
            string itemFileExtension = null;

            if (findData.cFileName.Contains('.'))
            {
                itemFileExtension = Path.GetExtension(itemPath);
                itemType = itemFileExtension.Trim('.') + " " + itemType;
            }

            bool itemThumbnailImgVis = false;
            bool itemEmptyImgVis = true;

            if (cancellationToken.IsCancellationRequested)
            {
                return null;
            }

            bool isHidden = ((FileAttributes)findData.dwFileAttributes & FileAttributes.Hidden) == FileAttributes.Hidden;
            double opacity = isHidden ? Constants.UI.DimItemOpacity : 1;

            // https://docs.microsoft.com/en-us/openspecs/windows_protocols/ms-fscc/c8e77b37-3909-4fe6-a4ea-2b9d423b1ee4
            bool isReparsePoint = ((FileAttributes)findData.dwFileAttributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint;
            bool isSymlink = isReparsePoint && findData.dwReserved0 == NativeFileOperationsHelper.IO_REPARSE_TAG_SYMLINK;

            if (isSymlink)
            {
                var targetPath = NativeFileOperationsHelper.ParseSymLink(itemPath);
                return new ShortcutItem(null, dateReturnFormat)
                {
                    PrimaryItemAttribute = StorageItemTypes.File,
                    FileExtension = itemFileExtension,
                    IsHiddenItem = isHidden,
                    Opacity = opacity,
                    FileImage = null,
                    LoadFileIcon = itemThumbnailImgVis,
                    LoadWebShortcutGlyph = false,
                    ItemNameRaw = itemName,
                    ItemDateModifiedReal = itemModifiedDate,
                    ItemDateAccessedReal = itemLastAccessDate,
                    ItemDateCreatedReal = itemCreatedDate,
                    ItemType = "ShortcutFileType".GetLocalized(),
                    ItemPath = itemPath,
                    FileSize = itemSize,
                    FileSizeBytes = itemSizeBytes,
                    TargetPath = targetPath,
                    IsSymLink = true
                };
            }
            else if (findData.cFileName.EndsWith(".lnk", StringComparison.Ordinal) || findData.cFileName.EndsWith(".url", StringComparison.Ordinal))
            {
                if (connection != null)
                {
                    var (status, response) = await connection.SendMessageForResponseAsync(new ValueSet()
                    {
                        { "Arguments", "FileOperation" },
                        { "fileop", "ParseLink" },
                        { "filepath", itemPath }
                    });
                    // If the request was canceled return now
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return null;
                    }
                    if (status == AppServiceResponseStatus.Success && response.ContainsKey("ShortcutInfo"))
                    {
                        var isUrl = findData.cFileName.EndsWith(".url", StringComparison.OrdinalIgnoreCase);
                        var shInfo = JsonConvert.DeserializeObject<ShellLinkItem>((string)response["ShortcutInfo"]);

                        return new ShortcutItem(null, dateReturnFormat)
                        {
                            PrimaryItemAttribute = shInfo.IsFolder ? StorageItemTypes.Folder : StorageItemTypes.File,
                            FileExtension = itemFileExtension,
                            IsHiddenItem = isHidden,
                            Opacity = opacity,
                            FileImage = null,
                            LoadFileIcon = !shInfo.IsFolder && itemThumbnailImgVis,
                            LoadWebShortcutGlyph = !shInfo.IsFolder && isUrl && itemEmptyImgVis,
                            ItemNameRaw = itemName,
                            ItemDateModifiedReal = itemModifiedDate,
                            ItemDateAccessedReal = itemLastAccessDate,
                            ItemDateCreatedReal = itemCreatedDate,
                            ItemType = isUrl ? "ShortcutWebLinkFileType".GetLocalized() : "ShortcutFileType".GetLocalized(),
                            ItemPath = itemPath,
                            FileSize = itemSize,
                            FileSizeBytes = itemSizeBytes,
                            TargetPath = shInfo.TargetPath,
                            Arguments = shInfo.Arguments,
                            WorkingDirectory = shInfo.WorkingDirectory,
                            RunAsAdmin = shInfo.RunAsAdmin,
                            IsUrl = isUrl,
                        };
                    }
                }
            }
            else if (App.LibraryManager.TryGetLibrary(itemPath, out LibraryLocationItem library))
            {
                return new LibraryItem(library)
                {
                    ItemDateModifiedReal = itemModifiedDate,
                    ItemDateCreatedReal = itemCreatedDate,
                };
            }
            else
            {
                if (ZipStorageFolder.IsZipPath(itemPath) && await ZipStorageFolder.CheckDefaultZipApp(itemPath))
                {
                    return new ZipItem(null, dateReturnFormat)
                    {
                        PrimaryItemAttribute = StorageItemTypes.Folder, // Treat zip files as folders
                        FileExtension = itemFileExtension,
                        FileImage = null,
                        LoadFileIcon = itemThumbnailImgVis,
                        ItemNameRaw = itemName,
                        IsHiddenItem = isHidden,
                        Opacity = opacity,
                        ItemDateModifiedReal = itemModifiedDate,
                        ItemDateAccessedReal = itemLastAccessDate,
                        ItemDateCreatedReal = itemCreatedDate,
                        ItemType = itemType,
                        ItemPath = itemPath,
                        FileSize = itemSize,
                        FileSizeBytes = itemSizeBytes
                    };
                }
                else
                {
                    return new ListedItem(null, dateReturnFormat)
                    {
                        PrimaryItemAttribute = StorageItemTypes.File,
                        FileExtension = itemFileExtension,
                        FileImage = null,
                        LoadFileIcon = itemThumbnailImgVis,
                        ItemNameRaw = itemName,
                        IsHiddenItem = isHidden,
                        Opacity = opacity,
                        ItemDateModifiedReal = itemModifiedDate,
                        ItemDateAccessedReal = itemLastAccessDate,
                        ItemDateCreatedReal = itemCreatedDate,
                        ItemType = itemType,
                        ItemPath = itemPath,
                        FileSize = itemSize,
                        FileSizeBytes = itemSizeBytes
                    };
                }
            }
            return null;
        }
    }
}
