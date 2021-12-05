using Files.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;

namespace Files.Filesystem.StorageItems
{
    public sealed class SystemStorageFile : BaseStorageFile
    {
        public StorageFile File { get; }

        public SystemStorageFile(StorageFile file)
        {
            File = file;
        }

        public override IAsyncOperation<IRandomAccessStream> OpenAsync(FileAccessMode accessMode)
        {
            return File.OpenAsync(accessMode);
        }

        public override IAsyncOperation<StorageStreamTransaction> OpenTransactedWriteAsync()
        {
            return File.OpenTransactedWriteAsync();
        }

        public override IAsyncOperation<BaseStorageFile> CopyAsync(IStorageFolder destinationFolder)
        {
            return CopyAsync(destinationFolder, Name, NameCollisionOption.FailIfExists);
        }

        public override IAsyncOperation<BaseStorageFile> CopyAsync(IStorageFolder destinationFolder, string desiredNewName)
        {
            return CopyAsync(destinationFolder, desiredNewName, NameCollisionOption.FailIfExists);
        }

        public override IAsyncOperation<BaseStorageFile> CopyAsync(IStorageFolder destinationFolder, string desiredNewName, NameCollisionOption option)
        {
            return AsyncInfo.Run<BaseStorageFile>(async (cancellationToken) =>
            {
                var destFolder = destinationFolder.AsBaseStorageFolder(); // Avoid calling IStorageFolder method
                if (destFolder is SystemStorageFolder sysFolder)
                {
                    // File created by CreateFileAsync will get immediately deleted on MTP?! (#7206)
                    return await File.CopyAsync(sysFolder.Folder, desiredNewName, option);
                }
                var destFile = await destFolder.CreateFileAsync(desiredNewName, option.Convert());
                using (var inStream = await this.OpenStreamForReadAsync())
                using (var outStream = await destFile.OpenStreamForWriteAsync())
                {
                    await inStream.CopyToAsync(outStream);
                    await outStream.FlushAsync();
                }
                return destFile;
            });
        }

        public override IAsyncAction CopyAndReplaceAsync(IStorageFile fileToReplace)
        {
            return AsyncInfo.Run(async (cancellationToken) =>
            {
                using (var inStream = await this.OpenStreamForReadAsync())
                using (var outStream = await fileToReplace.OpenStreamForWriteAsync())
                {
                    await inStream.CopyToAsync(outStream);
                    await outStream.FlushAsync();
                }
            });
        }

        public override IAsyncAction MoveAsync(IStorageFolder destinationFolder)
        {
            return File.MoveAsync(destinationFolder);
        }

        public override IAsyncAction MoveAsync(IStorageFolder destinationFolder, string desiredNewName)
        {
            return File.MoveAsync(destinationFolder, desiredNewName);
        }

        public override IAsyncAction MoveAsync(IStorageFolder destinationFolder, string desiredNewName, NameCollisionOption option)
        {
            return File.MoveAsync(destinationFolder, desiredNewName, option);
        }

        public override IAsyncAction MoveAndReplaceAsync(IStorageFile fileToReplace)
        {
            return File.MoveAndReplaceAsync(fileToReplace);
        }

        public override string ContentType => File.ContentType;

        public override string FileType => File.FileType;

        public override IAsyncAction RenameAsync(string desiredName)
        {
            return File.RenameAsync(desiredName);
        }

        public override IAsyncAction RenameAsync(string desiredName, NameCollisionOption option)
        {
            return File.RenameAsync(desiredName, option);
        }

        public override IAsyncAction DeleteAsync()
        {
            return File.DeleteAsync();
        }

        public override IAsyncAction DeleteAsync(StorageDeleteOption option)
        {
            return File.DeleteAsync(option);
        }

        public override IAsyncOperation<BaseBasicProperties> GetBasicPropertiesAsync()
        {
            return AsyncInfo.Run<BaseBasicProperties>(async (cancellationToken) =>
            {
                var basicProps = await File.GetBasicPropertiesAsync();
                return new SystemFileBasicProperties(basicProps);
            });
        }

        public override bool IsOfType(StorageItemTypes type)
        {
            return File.IsOfType(type);
        }

        public override Windows.Storage.FileAttributes Attributes => File.Attributes;

        public override DateTimeOffset DateCreated => File.DateCreated;

        public override string Name => File?.Name;

        public override string Path => File?.Path;

        public override IAsyncOperation<IRandomAccessStreamWithContentType> OpenReadAsync()
        {
            return File.OpenReadAsync();
        }

        public override IAsyncOperation<IInputStream> OpenSequentialReadAsync()
        {
            return File.OpenSequentialReadAsync();
        }

        public override IAsyncOperation<StorageItemThumbnail> GetThumbnailAsync(ThumbnailMode mode)
        {
            return File.GetThumbnailAsync(mode);
        }

        public override IAsyncOperation<StorageItemThumbnail> GetThumbnailAsync(ThumbnailMode mode, uint requestedSize)
        {
            return File.GetThumbnailAsync(mode, requestedSize);
        }

        public override IAsyncOperation<StorageItemThumbnail> GetThumbnailAsync(ThumbnailMode mode, uint requestedSize, ThumbnailOptions options)
        {
            return File.GetThumbnailAsync(mode, requestedSize, options);
        }

        public override IAsyncOperation<StorageFile> ToStorageFileAsync()
        {
            return Task.FromResult(File).AsAsyncOperation();
        }

        public override IAsyncOperation<BaseStorageFolder> GetParentAsync()
        {
            return AsyncInfo.Run<BaseStorageFolder>(async (cancellationToken) =>
            {
                return new SystemStorageFolder(await File.GetParentAsync());
            });
        }

        public override bool IsEqual(IStorageItem item) => File.IsEqual(item);

        public override IAsyncOperation<IRandomAccessStream> OpenAsync(FileAccessMode accessMode, StorageOpenOptions options) => File.OpenAsync(accessMode, options);

        public override IAsyncOperation<StorageStreamTransaction> OpenTransactedWriteAsync(StorageOpenOptions options) => File.OpenTransactedWriteAsync(options);

        public static IAsyncOperation<BaseStorageFile> FromPathAsync(string path)
        {
            return AsyncInfo.Run<BaseStorageFile>(async (cancellationToken) =>
            {
                return new SystemStorageFile(await StorageFile.GetFileFromPathAsync(path));
            });
        }

        public override string DisplayName => File?.DisplayName;

        public override string DisplayType => File?.DisplayType;

        public override string FolderRelativeId => File?.FolderRelativeId;

        public override IStorageItemExtraProperties Properties => File?.Properties;

        private class SystemFileBasicProperties : BaseBasicProperties
        {
            private IStorageItemExtraProperties basicProps;

            public SystemFileBasicProperties(IStorageItemExtraProperties basicProps)
            {
                this.basicProps = basicProps;
            }

            public override DateTimeOffset DateModified
            {
                get => (basicProps as BasicProperties)?.DateModified ?? DateTimeOffset.Now;
            }

            public override DateTimeOffset ItemDate
            {
                get => (basicProps as BasicProperties)?.ItemDate ?? DateTimeOffset.Now;
            }

            public override ulong Size
            {
                get => (basicProps as BasicProperties)?.Size ?? 0;
            }

            public override IAsyncOperation<IDictionary<string, object>> RetrievePropertiesAsync(IEnumerable<string> propertiesToRetrieve)
            {
                return basicProps.RetrievePropertiesAsync(propertiesToRetrieve);
            }

            public override IAsyncAction SavePropertiesAsync([HasVariant] IEnumerable<KeyValuePair<string, object>> propertiesToSave)
            {
                return basicProps.SavePropertiesAsync(propertiesToSave);
            }

            public override IAsyncAction SavePropertiesAsync()
            {
                return basicProps.SavePropertiesAsync();
            }
        }
    }
}
