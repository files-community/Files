using Files.Uwp.Helpers;
using Microsoft.Toolkit.Uwp;
using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using IO = System.IO;
using Windows.Storage.Search;
using System.Collections.Generic;
using System.Threading.Tasks;
using static Files.Uwp.Helpers.NativeFindStorageItemHelper;

namespace Files.Uwp.Filesystem.StorageItems
{
    /// <summary>
    /// Implements IStorageItem, allowing us to get an instance of IStorageItem for a ListedItem
    /// representing a standard filesystem item. As such, VirtualStorageItem does not support hidden, 
    /// shortcut, or link items.
    /// </summary>
    public class VirtualStorageItem
    {
        public static IStorageItem FromListedItem(ListedItem item)
        {
            if (item.IsZipItem || item.PrimaryItemAttribute == StorageItemTypes.File)
            {
                return new VirtualStorageFile(
                    name: item.ItemNameRaw,
                    path: item.ItemPath,
                    dateCreated: item.ItemDateCreatedReal);
            }
            else
            {
                return new VirtualStorageFolder(
                    name: item.ItemNameRaw,
                    path: item.ItemPath,
                    dateCreated: item.ItemDateCreatedReal);
            }
        }

        public static IStorageItem FromPath(string path)
        {
            FINDEX_INFO_LEVELS findInfoLevel = FINDEX_INFO_LEVELS.FindExInfoBasic;
            int additionalFlags = FIND_FIRST_EX_LARGE_FETCH;
            IntPtr hFile = FindFirstFileExFromApp(path, findInfoLevel, out WIN32_FIND_DATA findData, FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero, additionalFlags);
            if (hFile.ToInt64() != -1)
            {
                // https://docs.microsoft.com/en-us/openspecs/windows_protocols/ms-fscc/c8e77b37-3909-4fe6-a4ea-2b9d423b1ee4
                bool isReparsePoint = ((System.IO.FileAttributes)findData.dwFileAttributes & System.IO.FileAttributes.ReparsePoint) == System.IO.FileAttributes.ReparsePoint;
                bool isSymlink = isReparsePoint && findData.dwReserved0 == NativeFileOperationsHelper.IO_REPARSE_TAG_SYMLINK;
                bool isHidden = ((System.IO.FileAttributes)findData.dwFileAttributes & System.IO.FileAttributes.Hidden) == System.IO.FileAttributes.Hidden;
                bool isDirectory = ((System.IO.FileAttributes)findData.dwFileAttributes & System.IO.FileAttributes.Directory) == System.IO.FileAttributes.Directory;

                if (!(isHidden && isSymlink))
                {
                    DateTime itemCreatedDate;

                    try
                    {
                        FileTimeToSystemTime(ref findData.ftCreationTime, out SYSTEMTIME systemCreatedDateOutput);
                        itemCreatedDate = systemCreatedDateOutput.ToDateTime();
                    }
                    catch (ArgumentException)
                    {
                        // Invalid date means invalid findData, do not add to list
                        return null;
                    }

                    if (isDirectory)
                    {
                        return new VirtualStorageFolder(
                            name: findData.cFileName,
                            path: path,
                            dateCreated: itemCreatedDate);
                    }
                    else
                    {
                        return new VirtualStorageFile(
                            name: findData.cFileName,
                            path: path,
                            dateCreated: itemCreatedDate);
                    }
                }

                FindClose(hFile);
            }

            return null;
        }
    }

    public class VirtualStorageFile : BaseStorageFile
    {
        public override Windows.Storage.FileAttributes Attributes { get; } = Windows.Storage.FileAttributes.Normal;

        public override DateTimeOffset DateCreated { get; }

        public override string Name { get; }

        public override string Path { get; }

        public VirtualStorageFile(string name, string path, DateTimeOffset dateCreated)
            => (Name, Path, DateCreated) = (name, path, dateCreated);

        private async void StreamedFileWriter(StreamedFileDataRequest request)
        {
            try
            {
                using (var stream = request.AsStreamForWrite())
                {
                    await stream.FlushAsync();
                }
                request.Dispose();
            }
            catch (Exception)
            {
                request.FailAndClose(StreamedFileFailureMode.Incomplete);
            }
        }

        public override IAsyncAction RenameAsync(string desiredName)
            => throw new NotSupportedException();

        public override IAsyncAction RenameAsync(string desiredName, NameCollisionOption option)
            => throw new NotSupportedException();

        public override IAsyncAction DeleteAsync()
            => throw new NotSupportedException();

        public override IAsyncAction DeleteAsync(StorageDeleteOption option)
            => throw new NotSupportedException();

        public override IAsyncOperation<BaseBasicProperties> GetBasicPropertiesAsync()
        {
            return AsyncInfo.Run(async (cancellationToken) =>
            {
                return new BaseBasicProperties();
            });
        }

        public override bool IsOfType(StorageItemTypes type) => type is StorageItemTypes.File;

        public override string DisplayName => Name;
        public override string ContentType => "application/octet-stream";
        public override string FileType => IO.Path.GetExtension(Name);
        public override string FolderRelativeId => $"0\\{Name}";

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

        public override IStorageItemExtraProperties Properties => new BaseBasicStorageItemExtraProperties(this);

        public override IAsyncAction CopyAndReplaceAsync(IStorageFile fileToReplace)
            => throw new NotSupportedException();

        public override IAsyncOperation<BaseStorageFile> CopyAsync(IStorageFolder destinationFolder)
            => throw new NotSupportedException();

        public override IAsyncOperation<BaseStorageFile> CopyAsync(IStorageFolder destinationFolder, string desiredNewName)
            => throw new NotSupportedException();

        public override IAsyncOperation<BaseStorageFile> CopyAsync(IStorageFolder destinationFolder, string desiredNewName, NameCollisionOption option)
            => throw new NotSupportedException();

        public override IAsyncOperation<BaseStorageFolder> GetParentAsync()
            => throw new NotSupportedException();

        public override IAsyncOperation<StorageItemThumbnail> GetThumbnailAsync(ThumbnailMode mode)
            => throw new NotSupportedException();

        public override IAsyncOperation<StorageItemThumbnail> GetThumbnailAsync(ThumbnailMode mode, uint requestedSize)
            => throw new NotSupportedException();

        public override IAsyncOperation<StorageItemThumbnail> GetThumbnailAsync(ThumbnailMode mode, uint requestedSize, ThumbnailOptions options)
            => throw new NotSupportedException();

        public override bool IsEqual(IStorageItem item)
        {
            return item?.Path == Path;
        }

        public override IAsyncAction MoveAndReplaceAsync(IStorageFile fileToReplace)
            => throw new NotSupportedException();

        public override IAsyncAction MoveAsync(IStorageFolder destinationFolder)
            => throw new NotSupportedException();

        public override IAsyncAction MoveAsync(IStorageFolder destinationFolder, string desiredNewName)
            => throw new NotSupportedException();

        public override IAsyncAction MoveAsync(IStorageFolder destinationFolder, string desiredNewName, NameCollisionOption option)
            => throw new NotSupportedException();

        public override IAsyncOperation<IRandomAccessStream> OpenAsync(FileAccessMode accessMode)
            => throw new NotSupportedException();

        public override IAsyncOperation<IRandomAccessStream> OpenAsync(FileAccessMode accessMode, StorageOpenOptions options)
            => throw new NotSupportedException();

        public override IAsyncOperation<IRandomAccessStreamWithContentType> OpenReadAsync()
            => throw new NotSupportedException();

        public override IAsyncOperation<IInputStream> OpenSequentialReadAsync()
            => throw new NotSupportedException();

        public override IAsyncOperation<StorageStreamTransaction> OpenTransactedWriteAsync()
            => throw new NotSupportedException();

        public override IAsyncOperation<StorageStreamTransaction> OpenTransactedWriteAsync(StorageOpenOptions options)
            => throw new NotSupportedException();

        public override IAsyncOperation<StorageFile> ToStorageFileAsync()
        {
            return AsyncInfo.Run(async (cancellationToken) =>
            {
                return await StorageFile.CreateStreamedFileAsync(Name, StreamedFileWriter, null);
            });
        }
    }

    public class VirtualStorageFolder : BaseStorageFolder
    {
        public override Windows.Storage.FileAttributes Attributes { get; } = Windows.Storage.FileAttributes.Directory;

        public override DateTimeOffset DateCreated { get; }

        public override string Name { get; }

        public override string Path { get; }

        public VirtualStorageFolder(string name, string path, DateTimeOffset dateCreated)
            => (Name, Path, DateCreated) = (name, path, dateCreated);

        private async void StreamedFileWriter(StreamedFileDataRequest request)
        {
            try
            {
                using (var stream = request.AsStreamForWrite())
                {
                    await stream.FlushAsync();
                }
                request.Dispose();
            }
            catch (Exception)
            {
                request.FailAndClose(StreamedFileFailureMode.Incomplete);
            }
        }

        public override string DisplayName => Name;
        public override string FolderRelativeId => $"0\\{Name}";
        public override string DisplayType => "FileFolderListItem".GetLocalized();

        public override IStorageItemExtraProperties Properties => new BaseBasicStorageItemExtraProperties(this);

        public override bool AreQueryOptionsSupported(QueryOptions queryOptions) => false;
        public override bool IsCommonFileQuerySupported(CommonFileQuery query) => false;
        public override bool IsCommonFolderQuerySupported(CommonFolderQuery query) => false;

        public override IAsyncOperation<BaseStorageFile> CreateFileAsync(string desiredName)
            => throw new NotSupportedException();

        public override IAsyncOperation<BaseStorageFile> CreateFileAsync(string desiredName, CreationCollisionOption options)
            => throw new NotSupportedException();

        public override StorageFileQueryResult CreateFileQuery()
            => throw new NotSupportedException();

        public override StorageFileQueryResult CreateFileQuery(CommonFileQuery query)
            => throw new NotSupportedException();

        public override BaseStorageFileQueryResult CreateFileQueryWithOptions(QueryOptions queryOptions)
            => throw new NotSupportedException();

        public override IAsyncOperation<BaseStorageFolder> CreateFolderAsync(string desiredName)
            => throw new NotSupportedException();

        public override IAsyncOperation<BaseStorageFolder> CreateFolderAsync(string desiredName, CreationCollisionOption options)
            => throw new NotSupportedException();

        public override StorageFolderQueryResult CreateFolderQuery()
            => throw new NotSupportedException();

        public override StorageFolderQueryResult CreateFolderQuery(CommonFolderQuery query)
            => throw new NotSupportedException();

        public override BaseStorageFolderQueryResult CreateFolderQueryWithOptions(QueryOptions queryOptions)
            => throw new NotSupportedException();

        public override StorageItemQueryResult CreateItemQuery()
            => throw new NotSupportedException();

        public override BaseStorageItemQueryResult CreateItemQueryWithOptions(QueryOptions queryOptions)
            => throw new NotSupportedException();

        public override IAsyncAction DeleteAsync()
            => throw new NotSupportedException();

        public override IAsyncAction DeleteAsync(StorageDeleteOption option)
            => throw new NotSupportedException();

        public override IAsyncOperation<BaseBasicProperties> GetBasicPropertiesAsync()
        {
            return AsyncInfo.Run(async (cancellationToken) =>
            {
                return new BaseBasicProperties();
            });
        }

        public override IAsyncOperation<BaseStorageFile> GetFileAsync(string name)
            => throw new NotSupportedException();

        public override IAsyncOperation<IReadOnlyList<BaseStorageFile>> GetFilesAsync()
            => throw new NotSupportedException();

        public override IAsyncOperation<IReadOnlyList<BaseStorageFile>> GetFilesAsync(CommonFileQuery query)
            => throw new NotSupportedException();

        public override IAsyncOperation<IReadOnlyList<BaseStorageFile>> GetFilesAsync(CommonFileQuery query, uint startIndex, uint maxItemsToRetrieve)
            => throw new NotSupportedException();

        public override IAsyncOperation<BaseStorageFolder> GetFolderAsync(string name)
            => throw new NotSupportedException();

        public override IAsyncOperation<IReadOnlyList<BaseStorageFolder>> GetFoldersAsync()
            => throw new NotSupportedException();

        public override IAsyncOperation<IReadOnlyList<BaseStorageFolder>> GetFoldersAsync(CommonFolderQuery query)
            => throw new NotSupportedException();

        public override IAsyncOperation<IReadOnlyList<BaseStorageFolder>> GetFoldersAsync(CommonFolderQuery query, uint startIndex, uint maxItemsToRetrieve)
            => throw new NotSupportedException();

        public override IAsyncOperation<IndexedState> GetIndexedStateAsync() => Task.FromResult(IndexedState.NotIndexed).AsAsyncOperation();

        public override IAsyncOperation<IStorageItem> GetItemAsync(string name)
            => throw new NotSupportedException();

        public override IAsyncOperation<IReadOnlyList<IStorageItem>> GetItemsAsync()
            => throw new NotSupportedException();

        public override IAsyncOperation<IReadOnlyList<IStorageItem>> GetItemsAsync(uint startIndex, uint maxItemsToRetrieve)
            => throw new NotSupportedException();

        public override IAsyncOperation<BaseStorageFolder> GetParentAsync()
            => throw new NotSupportedException();

        public override IAsyncOperation<StorageItemThumbnail> GetThumbnailAsync(ThumbnailMode mode)
            => throw new NotSupportedException();

        public override IAsyncOperation<StorageItemThumbnail> GetThumbnailAsync(ThumbnailMode mode, uint requestedSize)
            => throw new NotSupportedException();

        public override IAsyncOperation<StorageItemThumbnail> GetThumbnailAsync(ThumbnailMode mode, uint requestedSize, ThumbnailOptions options)
            => throw new NotSupportedException();

        public override bool IsEqual(IStorageItem item)
        {
            return item?.Path == Path;
        }

        public override bool IsOfType(StorageItemTypes type) => type is StorageItemTypes.Folder;

        public override IAsyncAction RenameAsync(string desiredName)
            => throw new NotSupportedException();

        public override IAsyncAction RenameAsync(string desiredName, NameCollisionOption option)
            => throw new NotSupportedException();

        public override IAsyncOperation<IStorageItem> TryGetItemAsync(string name)
            => throw new NotSupportedException();

        public override IAsyncOperation<StorageFolder> ToStorageFolderAsync() => throw new NotSupportedException();

        public override IAsyncOperation<IStorageItem> ToStorageItemAsync()
        {
            return AsyncInfo.Run<IStorageItem>(async (cancellationToken) =>
            {
                return await StorageFile.CreateStreamedFileAsync(Name, StreamedFileWriter, null);
            });
        }
    }
}
