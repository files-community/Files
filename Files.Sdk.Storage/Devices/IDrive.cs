namespace Files.Sdk.Storage.Devices
{
    public interface IDrive : IDevice
    {
        string VolumeLabel { get; }

        long AvailableFreeSpace { get; }

        long TotalFreeSpace { get; }

        long TotalSize { get; }
    }
}
