using CommunityToolkit.Mvvm.DependencyInjection;
using Files.Backend.Services.Settings;
using Files.Extensions;
using Files.Shared.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using static Files.Helpers.NativeFindStorageItemHelper;

namespace Files.Filesystem
{
    public interface IFolderSizeProvider
    {
        public event EventHandler<FolderSizeChangedEventArgs> FolderSizeChanged;

        Task CleanCacheAsync();
        void UpdateFolder(ListedItem folder, CancellationToken cancellationToken);
    }

    public class FolderSizeChangedEventArgs : EventArgs
    {
        public ListedItem Folder { get; }

        public FolderSizeChangedEventArgs(ListedItem folder) => Folder = folder;
    }

    internal class FolderSizeProvider : IFolderSizeProvider, IDisposable
    {
        private readonly IPreferencesSettingsService preferencesSettingsService = Ioc.Default.GetService<IPreferencesSettingsService>();

        private readonly IDictionary<string, long> cacheSizes = new Dictionary<string, long>();

        public event EventHandler<FolderSizeChangedEventArgs> FolderSizeChanged;

        private bool showFolderSize;

        public FolderSizeProvider()
        {
            showFolderSize = preferencesSettingsService.ShowFolderSize;
            preferencesSettingsService.PropertyChanged += PreferencesSettingsService_PropertyChanged;
        }

        public async Task CleanCacheAsync()
        {
            if (!showFolderSize)
            {
                return; // The cache is already empty.
            }

            var drives = DriveInfo.GetDrives().Select(drive => drive.Name).ToArray();

            await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(() =>
            {
                var oldPaths = cacheSizes.Keys.Where(path => !drives.Any(drive => path.StartsWith(drive))).ToList();
                foreach (var oldPath in oldPaths)
                {
                    cacheSizes.Remove(oldPath);
                }
            }, Windows.System.DispatcherQueuePriority.Low);
        }

        public async void UpdateFolder(ListedItem folder, CancellationToken cancellationToken)
        {
            if (!preferencesSettingsService.ShowFolderSize)
            {
                return;
            }

            if (folder.PrimaryItemAttribute == Windows.Storage.StorageItemTypes.Folder && folder.ContainsFilesOrFolders)
            {
                await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(() =>
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
                        RaiseSizeChanged(folder);
                    }
                }, Windows.System.DispatcherQueuePriority.Low);

                long size = await Calculate(folder.ItemPath);

                await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(() =>
                {
                    cacheSizes[folder.ItemPath] = size;
                    folder.FileSizeBytes = size;
                    folder.FileSize = size.ToSizeString();
                    RaiseSizeChanged(folder);
                }, Windows.System.DispatcherQueuePriority.Low);
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
                            await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(() =>
                            {
                                cacheSizes[localPath] = localSize;

                                if (size > folder.FileSizeBytes)
                                {
                                    folder.FileSizeBytes = size;
                                    folder.FileSize = size.ToSizeString();
                                    RaiseSizeChanged(folder);
                                };
                            }, Windows.System.DispatcherQueuePriority.Low);
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

        private void RaiseSizeChanged(ListedItem folder)
            => FolderSizeChanged?.Invoke(this, new FolderSizeChangedEventArgs(folder));

        private void PreferencesSettingsService_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(IPreferencesSettingsService.ShowFolderSize))
            {
                showFolderSize = preferencesSettingsService.ShowFolderSize;
                if (!showFolderSize)
                {
                    cacheSizes.Clear();
                }
                RaiseSizeChanged(null);
            }
        }

        public void Dispose()
        {
            preferencesSettingsService.PropertyChanged -= PreferencesSettingsService_PropertyChanged;
        }
    }
}