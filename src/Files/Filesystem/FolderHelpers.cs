using Files.Extensions;
using Files.Filesystem.StorageItems;
using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using static Files.Helpers.NativeFindStorageItemHelper;

namespace Files.Filesystem
{
    public static class FolderHelpers
    {
        private static readonly CoreDispatcher dispatcher = CoreApplication.MainView.CoreWindow.Dispatcher;

        private static readonly IDictionary<string, long> cacheSizes = new Dictionary<string, long>();

        public static bool CheckFolderAccessWithWin32(string path)
        {
            IntPtr hFileTsk = FindFirstFileExFromApp($"{path}{Path.DirectorySeparatorChar}*.*", FINDEX_INFO_LEVELS.FindExInfoBasic,
                out WIN32_FIND_DATA _, FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero, FIND_FIRST_EX_LARGE_FETCH);
            if (hFileTsk.ToInt64() != -1)
            {
                FindClose(hFileTsk);
                return true;
            }
            return false;
        }

        public static async Task<bool> CheckBitlockerStatusAsync(BaseStorageFolder rootFolder, string path)
        {
            if (rootFolder?.Properties is null)
            {
                return false;
            }
            if (Path.IsPathRooted(path) && Path.GetPathRoot(path) == path)
            {
                IDictionary<string, object> extraProperties =
                    await rootFolder.Properties.RetrievePropertiesAsync(new string[] { "System.Volume.BitLockerProtection" });
                return (int?)extraProperties["System.Volume.BitLockerProtection"] == 6; // Drive is bitlocker protected and locked
            }
            return false;
        }

        /// <summary>
        /// This function is used to determine whether or not a folder has any contents.
        /// </summary>
        /// <param name="targetPath">The path to the target folder</param>
        ///
        public static bool CheckForFilesFolders(string targetPath)
        {
            IntPtr hFile = FindFirstFileExFromApp($"{targetPath}{Path.DirectorySeparatorChar}*.*", FINDEX_INFO_LEVELS.FindExInfoBasic,
                out WIN32_FIND_DATA _, FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero, FIND_FIRST_EX_LARGE_FETCH);
            FindNextFile(hFile, out _);
            var result = FindNextFile(hFile, out _);
            FindClose(hFile);
            return result;
        }

        public static async void UpdateFolder(ListedItem folder, CancellationToken cancellationToken)
        {
            if (folder.PrimaryItemAttribute == Windows.Storage.StorageItemTypes.Folder && folder.ContainsFilesOrFolders)
            {
                await dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                {
                    if (cacheSizes.ContainsKey(folder.ItemPath))
                    {
                        long size = cacheSizes[folder.ItemPath];
                        folder.FileSizeBytes = size;
                        folder.FileSize = size.ToSizeString();
                    }
                    else
                    {
                        folder.FileSizeBytes = 0;
                        folder.FileSize = "ItemSizeNotCalculated".GetLocalized();
                    }
                });

                long size = await Calculate(folder.ItemPath);

                await dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                {
                    cacheSizes[folder.ItemPath] = size;
                    folder.FileSizeBytes = size;
                    folder.FileSize = size.ToSizeString();
                });
            }

            async Task<long> Calculate(string folderPath, int level = 0)
            {
                if (string.IsNullOrEmpty(folderPath))
                {
                    return 0;
                }

                IntPtr hFile = FindFirstFileExFromApp($"{folderPath}{Path.DirectorySeparatorChar}*.*", FINDEX_INFO_LEVELS.FindExInfoBasic,
                    out WIN32_FIND_DATA findData, FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero, FIND_FIRST_EX_LARGE_FETCH);

                long size = 0;
                long localSize = 0;
                string localPath = string.Empty;

                if (hFile.ToInt64() != -1)
                {
                    do
                    {
                        bool isDirectory = ((FileAttributes)findData.dwFileAttributes & FileAttributes.Directory) == FileAttributes.Directory;
                        if (!isDirectory)
                        {
                            size += findData.GetSize();
                        }
                        else if (findData.cFileName is not "." and not "..")
                        {
                            localPath = Path.Combine(folderPath, findData.cFileName);
                            localSize = await Calculate(localPath, level + 1);
                            size += localSize;
                        }

                        if (level <= 3)
                        {
                            await dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                            {
                                cacheSizes[localPath] = localSize;

                                if (size > folder.FileSizeBytes)
                                {
                                    folder.FileSizeBytes = size;
                                    folder.FileSize = size.ToSizeString();
                                };
                            });
                        }

                        if (cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }
                    } while (FindNextFile(hFile, out findData));
                    FindClose(hFile);
                }
                return size;
            }
        }

        public static async void CleanCache()
        {
            var drives = DriveInfo.GetDrives().Select(drive => drive.Name).ToArray();

            await dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
            {
                var oldPaths = cacheSizes.Keys.Where(path => !drives.Any(drive => path.StartsWith(drive))).ToList();
                foreach (var oldPath in oldPaths)
                {
                    cacheSizes.Remove(oldPath);
                }
            });
        }
    }
}
