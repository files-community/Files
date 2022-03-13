using System;

namespace Files.Backend.Item
{
    public interface IDriveItemViewModel : IItemViewModel
    {
        bool IsFixed { get; }
        bool IsRemovable { get; }
        bool IsNetwork { get; }
        bool IsRam { get; }
        bool IsCDRom { get; }
        bool IsFloppyDisk { get; }
        bool IsNoRootDirectory { get; }
        bool IsVirtual { get; }
        bool IsCloud { get; }

        ByteSize UsedSpace { get; }
        float UsedSpacePercent { get; }

        ByteSize FreeSpace { get; }
        float FreeSpacePercent { get; }

        ByteSize TotalSpace { get; }
        string SpaceText { get; }

        Uri? ImageSource { get; }
        byte[]? ImageBytes { get; }
    }
}
