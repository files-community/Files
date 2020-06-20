using ByteSizeLib;
using Files.Helpers;
using System;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml;

namespace Files.Filesystem
{
    public class DriveItem : INavigationControlItem
    {
        public string Glyph { get; set; }
        public string Text { get; set; }
        public string Path { get; set; }
        public NavigationControlItemType ItemType { get; set; } = NavigationControlItemType.Drive;
        public ByteSize MaxSpace { get; set; }
        public ByteSize FreeSpace { get; set; }
        public ByteSize SpaceUsed { get; set; }
        public string SpaceText { get; set; }
        public Visibility ItemVisibility { get; set; } = Visibility.Visible;

        private DriveType _type;

        public DriveType Type
        {
            get => _type;
            set
            {
                _type = value;
                SetGlyph(_type);
            }
        }

        public DriveItem()
        {
            ItemType = NavigationControlItemType.OneDrive;
        }

        public DriveItem(StorageFolder root, DriveType type)
        {
            Text = root.DisplayName;
            Type = type;
            Path = root.Path;

            var properties = Task.Run(async () =>
            {
                return await root.Properties.RetrievePropertiesAsync(new[] { "System.FreeSpace", "System.Capacity" });
            }).Result;

            try
            {           
                MaxSpace = ByteSize.FromBytes((ulong)properties["System.Capacity"]);
                FreeSpace = ByteSize.FromBytes((ulong)properties["System.FreeSpace"]);

                SpaceUsed = MaxSpace - FreeSpace;
                SpaceText = string.Format(
                    ResourceController.GetTranslation("DriveFreeSpaceAndCapacity"),
                    FreeSpace.ToBinaryString().ConvertSizeAbbreviation(),
                    MaxSpace.ToBinaryString().ConvertSizeAbbreviation());
            }
            catch (NullReferenceException)
            {
                SpaceText = "Unknown";
            }
        }

        private void SetGlyph(DriveType type)
        {
            switch (type)
            {
                case DriveType.Fixed:
                    Glyph = "\uEDA2";
                    break;

                case DriveType.Removable:
                    Glyph = "\uE88E";
                    break;

                case DriveType.Network:
                    Glyph = "\uE8CE";
                    break;

                case DriveType.Ram:
                    break;

                case DriveType.CDRom:
                    Glyph = "\uE958";
                    break;

                case DriveType.Unknown:
                    break;

                case DriveType.NoRootDirectory:
                    break;

                case DriveType.VirtualDrive:
                    Glyph = "\uE753";
                    break;

                case DriveType.FloppyDisk:
                    Glyph = "\uEDA2";
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }

    public enum DriveType
    {
        Fixed,
        Removable,
        Network,
        Ram,
        CDRom,
        FloppyDisk,
        Unknown,
        NoRootDirectory,
        VirtualDrive
    }
}