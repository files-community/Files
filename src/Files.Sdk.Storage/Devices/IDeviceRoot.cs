﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Files.Sdk.Storage.Services;

namespace Files.Sdk.Storage.Devices
{
    public interface IDeviceRoot
    {
        Task<IFolder?> GetFolderRootAsync();

        Task<IEnumerable<IStorable>?> GetStorageRootAsync();

        Task<IFileSystemService?> GetFileSystemServiceAsync();
    }
}
