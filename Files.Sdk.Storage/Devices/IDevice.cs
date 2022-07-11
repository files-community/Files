using System.Threading.Tasks;
using Files.Sdk.Storage.StorageProperties;

namespace Files.Sdk.Storage.Devices
{
    public interface IDevice
    {
        string Name { get; }

        IStoragePropertiesCollection? Properties { get; }

        Task<bool> PingAsync();

        Task<IDeviceRoot?> GetRootAsync();
    }
}
