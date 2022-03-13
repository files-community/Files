using System;

namespace Files.Backend.Item
{
    internal class DriveItemViewModel : IDriveItemViewModel
    {
        private readonly IDriveItem item;

        public string Path => item.Path;
        public string Name => item.Name;

        public bool IsFixed => item.DriveType is DriveTypes.Fixed;
        public bool IsRemovable => item.DriveType is DriveTypes.Removable or DriveTypes.CDRom;
        public bool IsNetwork => item.DriveType is DriveTypes.Network;
        public bool IsRam => item.DriveType is DriveTypes.Ram;
        public bool IsCDRom => item.DriveType is DriveTypes.CDRom;
        public bool IsFloppyDisk => item.DriveType is DriveTypes.FloppyDisk;
        public bool IsNoRootDirectory => item.DriveType is DriveTypes.NoRootDirectory;
        public bool IsVirtual => item.DriveType is DriveTypes.Virtual;
        public bool IsCloud => item.DriveType is DriveTypes.Cloud;

        public ByteSize UsedSpace => item.UsedSpace;
        public ByteSize FreeSpace => item.FreeSpace;
        public ByteSize TotalSpace => item.TotalSpace;

        public float UsedSpacePercent => GetSpacePercent(UsedSpace, TotalSpace);
        public float FreeSpacePercent => GetSpacePercent(FreeSpace, TotalSpace);

        public string SpaceText
            => string.Format("DriveFreeSpaceAndCapacity".ToLocalized(), FreeSpace, TotalSpace);

        public Uri? ImageSource => item.ImageSource;
        public byte[]? ImageBytes => item.ImageBytes;

        public DriveItemViewModel(IDriveItem item) => this.item = item;

        private static float GetSpacePercent(ByteSize space, ByteSize total)
            => total == ByteSize.Zero ? 0f : (float)space.Bytes / total.Bytes;
    }
}
