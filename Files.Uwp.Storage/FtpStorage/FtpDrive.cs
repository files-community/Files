using Files.Sdk.Storage.Devices;
using System.Threading.Tasks;
using Files.Sdk.Storage.StorageProperties;

#nullable enable

namespace Files.Uwp.Storage.FtpStorage
{
    public sealed class FtpDrive : IDrive
    {
        public string Name { get; }

        public IStoragePropertiesCollection? Properties { get; }

        public string VolumeLabel { get; }

        public long AvailableFreeSpace { get; }

        public long TotalFreeSpace { get; }

        public long TotalSize { get; }

        public FtpDrive(string name, string volumeLabel)
        {
            Name = name;
            VolumeLabel = volumeLabel;
        }

        public Task<bool> PingAsync()
        {
            return Task.FromResult(true); // TODO: Determine if the FTP drive is accessible and refresh drive data
        }

        public Task<IDeviceRoot?> GetRootAsync()
        {
            return Task.FromResult<IDeviceRoot?>(null); // TODO: Implement the device root
        }
    }
}
