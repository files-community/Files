using Files.Uwp.Filesystem.StorageItems;
using Microsoft.Toolkit.Uwp;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using static Files.Uwp.Filesystem.Native.NativeApi;
using static Files.Uwp.Filesystem.Native.NativeHelpers;
using IO = System.IO;
using S = Windows.Storage;

namespace Files.Uwp.Filesystem.Native
{
    /// <summary>
    /// Shortcuts and alternate data stream.
    /// Uses *FromApp methods for file operations
    /// </summary>
    public class NativeStorageFile : BaseStorageFile
    {
        public override string Path { get; }
        public override string Name { get; }
        public override string DisplayName => Name;
        public override string ContentType => "application/octet-stream";
        public override string FileType => IO.Path.GetExtension(Name);
        public override string FolderRelativeId => $"0\\{Name}";

        public bool IsShortcut => FileType.ToLowerInvariant() is ".lnk" or ".url";
        public bool IsAlternateStream => Regex.IsMatch(Path, @"\w:\w");

        public override string DisplayType
        {
            get
            {
                var itemType = "ItemTypeFile".GetLocalized();
                if (Name.Contains(".", StringComparison.Ordinal))
                {
                    itemType = IO.Path.GetExtension(Name).Trim('.') + " " + itemType;
                }
                return itemType;
            }
        }

        public override DateTimeOffset DateCreated { get; }
        public override S.FileAttributes Attributes => S.FileAttributes.Normal;
        public override IStorageItemExtraProperties Properties => new BaseBasicStorageItemExtraProperties(this);

        public NativeStorageFile(string path, string name, DateTimeOffset dateCreated)
        {
            Path = path;
            Name = name;
            DateCreated = dateCreated;
        }

        public override IAsyncOperation<StorageFile> ToStorageFileAsync() => throw new NotSupportedException();

        public override IAsyncAction CopyAndReplaceAsync(IStorageFile fileToReplace)
            => throw new NotSupportedException();

        public override IAsyncOperation<BaseStorageFile> CopyAsync(IStorageFolder destinationFolder)
            => CopyAsync(destinationFolder, Name, NameCollisionOption.FailIfExists);

        public override IAsyncOperation<BaseStorageFile> CopyAsync(IStorageFolder destinationFolder, string desiredNewName)
            => CopyAsync(destinationFolder, desiredNewName, NameCollisionOption.FailIfExists);

        public override IAsyncOperation<BaseStorageFile> CopyAsync(IStorageFolder destinationFolder, string desiredNewName, NameCollisionOption option)
        {
            return AsyncInfo.Run<BaseStorageFile>(async (cancellationToken) =>
            {
                if (string.IsNullOrEmpty(destinationFolder.Path))
                {
                    throw new NotSupportedException();
                }
                var destination = IO.Path.Combine(destinationFolder.Path, desiredNewName);
                var destFile = new NativeStorageFile(destination, desiredNewName, DateTime.Now);
                if (!IsAlternateStream)
                {
                    if (!await Task.Run(() => CopyFileFromApp(Path, destination, option is not NameCollisionOption.ReplaceExisting)))
                    {
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    }
                }
                else
                {
                    destFile.CreateFile();
                    using var inStream = await this.OpenStreamForReadAsync();
                    using var outStream = await destFile.OpenStreamForWriteAsync();

                    await inStream.CopyToAsync(outStream);
                    await outStream.FlushAsync();
                }
                return destFile;
            });
        }

        private void CreateFile()
        {
            using var hFile = CreateFileForWrite(Path, false);
            if (hFile.IsInvalid)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }

        public override IAsyncAction DeleteAsync()
        {
            return AsyncInfo.Run(async (cancellationToken) =>
            {
                if (!DeleteFileFromApp(Path))
                {
                    await Task.Yield();
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
            });
        }

        public override IAsyncAction DeleteAsync(StorageDeleteOption option)
        {
            if (option is StorageDeleteOption.PermanentDelete)
            {
                return DeleteAsync();
            }
            throw new NotSupportedException();
        }

        public override IAsyncOperation<BaseBasicProperties> GetBasicPropertiesAsync()
        {
            return AsyncInfo.Run(async (cancellationToken) =>
            {
                await Task.Yield();
                return new BaseBasicProperties();
            });
        }

        public override IAsyncOperation<BaseStorageFolder> GetParentAsync()
            => throw new NotSupportedException();

        public override IAsyncOperation<StorageItemThumbnail> GetThumbnailAsync(ThumbnailMode mode)
            => throw new NotSupportedException();

        public override IAsyncOperation<StorageItemThumbnail> GetThumbnailAsync(ThumbnailMode mode, uint requestedSize)
            => throw new NotSupportedException();

        public override IAsyncOperation<StorageItemThumbnail> GetThumbnailAsync(ThumbnailMode mode, uint requestedSize, ThumbnailOptions options)
            => throw new NotSupportedException();

        public static IAsyncOperation<BaseStorageFile> FromPathAsync(string path)
        {
            return AsyncInfo.Run<BaseStorageFile>(async (cancellationToken) =>
            {
                await Task.Yield();
                if (IsNativePath(path))
                {
                    if (CheckAccess(path))
                    {
                        var name = IO.Path.GetFileName(path);
                        return new NativeStorageFile(path, name.Substring(name.LastIndexOf(":") + 1), DateTime.Now);
                    }
                }
                return null;
            });
        }

        private static bool CheckAccess(string path)
        {
            using var hFile = OpenFileForRead(path);
            return !hFile.IsInvalid;
        }

        private static bool IsNativePath(string path)
        {
            var isShortcut = path.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".url", StringComparison.OrdinalIgnoreCase);
            var isAlternateStream = Regex.IsMatch(path, @"\w:\w");
            return isShortcut || isAlternateStream;
        }

        public override bool IsEqual(IStorageItem item) => item?.Path == Path;
        public override bool IsOfType(StorageItemTypes type) => type is StorageItemTypes.File;

        public override IAsyncAction MoveAndReplaceAsync(IStorageFile fileToReplace)
            => throw new NotSupportedException();

        public override IAsyncAction MoveAsync(IStorageFolder destinationFolder)
            => MoveAsync(destinationFolder, Name, NameCollisionOption.FailIfExists);

        public override IAsyncAction MoveAsync(IStorageFolder destinationFolder, string desiredNewName)
            => MoveAsync(destinationFolder, desiredNewName, NameCollisionOption.FailIfExists);

        public override IAsyncAction MoveAsync(IStorageFolder destinationFolder, string desiredNewName, NameCollisionOption option)
        {
            return AsyncInfo.Run(async (cancellationToken) =>
            {
                if (string.IsNullOrEmpty(destinationFolder.Path))
                {
                    throw new NotSupportedException();
                }
                var destination = IO.Path.Combine(destinationFolder.Path, desiredNewName);
                if (!IsAlternateStream)
                {
                    if (!await Task.Run(() => NativeApi.MoveFileFromApp(Path, destination)))
                    {
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    }
                }
                else
                {
                    await CopyAsync(destinationFolder, desiredNewName, option);
                    await DeleteAsync();
                }
            });
        }

        public override IAsyncOperation<IRandomAccessStream> OpenAsync(FileAccessMode accessMode)
        {
            return AsyncInfo.Run(async (cancellationToken) =>
            {
                await Task.Yield();
                var hFile = NativeHelpers.OpenFileForRead(Path, accessMode == FileAccessMode.ReadWrite);
                return new FileStream(hFile, accessMode == FileAccessMode.ReadWrite ? FileAccess.ReadWrite : FileAccess.Read).AsRandomAccessStream();
            });
        }

        public override IAsyncOperation<IRandomAccessStream> OpenAsync(FileAccessMode accessMode, StorageOpenOptions options) => OpenAsync(accessMode);

        public override IAsyncOperation<IRandomAccessStreamWithContentType> OpenReadAsync()
        {
            return AsyncInfo.Run<IRandomAccessStreamWithContentType>(async (cancellationToken) =>
            {
                await Task.Yield();
                return new StreamWithContentType(await OpenAsync(FileAccessMode.Read));
            });
        }

        public override IAsyncOperation<IInputStream> OpenSequentialReadAsync()
        {
            return AsyncInfo.Run(async (cancellationToken) =>
            {
                await Task.Yield();
                var hFile = OpenFileForRead(Path);
                return new FileStream(hFile, FileAccess.Read).AsInputStream();
            });
        }

        public override IAsyncOperation<StorageStreamTransaction> OpenTransactedWriteAsync()
            => throw new NotSupportedException();

        public override IAsyncOperation<StorageStreamTransaction> OpenTransactedWriteAsync(StorageOpenOptions options)
            => throw new NotSupportedException();

        public override IAsyncAction RenameAsync(string desiredName)
            => RenameAsync(desiredName, NameCollisionOption.FailIfExists);

        public override IAsyncAction RenameAsync(string desiredName, NameCollisionOption option)
        {
            return AsyncInfo.Run(async (cancellationToken) =>
            {
                string destination = IO.Path.Combine(IO.Path.GetDirectoryName(Path), desiredName);
                var destFile = new NativeStorageFile(destination, desiredName, DateTime.Now);
                if (!IsAlternateStream)
                {
                    if (!await Task.Run(() => MoveFileFromApp(Path, destination)))
                    {
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    }
                }
                else
                {
                    destFile.CreateFile();
                    using (var inStream = await this.OpenStreamForReadAsync())
                    using (var outStream = await destFile.OpenStreamForWriteAsync())
                    {
                        await inStream.CopyToAsync(outStream);
                        await outStream.FlushAsync();
                    }
                    await DeleteAsync();
                }
            });
        }
    }
}
