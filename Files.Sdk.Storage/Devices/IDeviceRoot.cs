using System.Collections.Generic;
using System.Threading.Tasks;

namespace Files.Sdk.Storage.Devices
{
    public interface IDeviceRoot
    {
        Task<object?> GetUnknownRootAsync();

        Task<IFolder?> GetRootAsFolderAsync();

        Task<IEnumerable<IBaseStorage>?> GetStorageRootAsync();
    }
}
