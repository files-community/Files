﻿using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace Files.Uwp.Filesystem.StorageItems
{
    public interface IBaseStorageFile : IStorageItem2, IStorageFile, IStorageFile2
        , IStorageItemProperties2, IStorageItemPropertiesWithProvider, IStorageFilePropertiesWithAvailability
    {
        new IStorageItemExtraProperties Properties { get; }

        IAsyncOperation<StorageFile> ToStorageFileAsync();

        new IAsyncOperation<BaseStorageFolder> GetParentAsync();
        new IAsyncOperation<BaseBasicProperties> GetBasicPropertiesAsync();

        new IAsyncOperation<BaseStorageFile> CopyAsync(IStorageFolder destinationFolder);
        new IAsyncOperation<BaseStorageFile> CopyAsync(IStorageFolder destinationFolder, string desiredNewName);
        new IAsyncOperation<BaseStorageFile> CopyAsync(IStorageFolder destinationFolder, string desiredNewName, NameCollisionOption option);
    }
}
