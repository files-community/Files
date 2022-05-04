using Files.Uwp.Extensions;
using Files.Backend.Services.Settings;
using CommunityToolkit.Mvvm.DependencyInjection;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using static Files.Uwp.Helpers.NativeFindStorageItemHelper;

namespace Files.Uwp.Filesystem
{
    public interface IFolderSizeProvider
    {
        public event EventHandler<FolderSizeChangedEventArgs> FolderSizeChanged;

        Task CleanCacheAsync();
        Task UpdateFolderAsync(string folderPath, CancellationToken cancellationToken);
    }

    public class FolderSizeChangedEventArgs : EventArgs
    {
        public string Folder { get; }
        public long Size { get; }
        public bool Intermediate { get; }

        public FolderSizeChangedEventArgs(string folderPath, long newSize, bool intermediate)
            => (Folder, Size, Intermediate) = (folderPath, newSize, intermediate);
    }

    public class FolderSizeProvider : IFolderSizeProvider, IDisposable
    {
        private readonly IPreferencesSettingsService preferencesSettingsService = Ioc.Default.GetService<IPreferencesSettingsService>();

        private readonly ConcurrentDictionary<string, long> cacheSizes = new ConcurrentDictionary<string, long>();

        public event EventHandler<FolderSizeChangedEventArgs> FolderSizeChanged;

        private bool showFolderSize;

        public FolderSizeProvider()
        {
            showFolderSize = preferencesSettingsService.ShowFolderSize;
            preferencesSettingsService.PropertyChanged += PreferencesSettingsService_PropertyChanged;
        }

        public Task CleanCacheAsync()
        {
            if (!showFolderSize)
            {
                return Task.CompletedTask; // The cache is already empty.
            }

            var drives = DriveInfo.GetDrives().Select(drive => drive.Name).ToArray();
            var oldPaths = cacheSizes.Keys.Where(path => !drives.Any(drive => path.StartsWith(drive))); // Keys return a snapshot
            foreach (var oldPath in oldPaths)
            {
                cacheSizes.TryRemove(oldPath, out _);
            }

            return Task.CompletedTask;
        }

        public async Task UpdateFolderAsync(string folderPath, CancellationToken cancellationToken)
        {
            if (!preferencesSettingsService.ShowFolderSize)
            {
                return;
            }

            await Task.Yield();
            if (cacheSizes.ContainsKey(folderPath))
            {
                long cachedSize = cacheSizes[folderPath];
                RaiseSizeChanged(folderPath, cachedSize);
            }
            else
            {
                RaiseSizeChanged(folderPath, -1);
            }

            long size = await Calculate(folderPath);

            cacheSizes[folderPath] = size;
            RaiseSizeChanged(folderPath, size);

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
                            await Task.Yield();
                            cacheSizes[localPath] = localSize;
                        }
                        if (level == 0)
                        {
                            RaiseSizeChanged(folderPath, size, true);
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

        private void RaiseSizeChanged(string folderPath, long newSize, bool intermediate = false)
            => FolderSizeChanged?.Invoke(this, new FolderSizeChangedEventArgs(folderPath, newSize, intermediate));

        private void PreferencesSettingsService_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(IPreferencesSettingsService.ShowFolderSize))
            {
                showFolderSize = preferencesSettingsService.ShowFolderSize;
                if (!showFolderSize)
                {
                    cacheSizes.Clear();
                }
                RaiseSizeChanged(null, -1);
            }
        }

        public void Dispose()
        {
            preferencesSettingsService.PropertyChanged -= PreferencesSettingsService_PropertyChanged;
        }
    }
}