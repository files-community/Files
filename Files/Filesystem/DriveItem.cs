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
        public ulong MaxSpace { get; set; } = 0;
        public ulong SpaceUsed { get; set; } = 0;
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
                SpaceUsed = MaxSpace -
                            Convert.ToUInt64(ByteSizeLib.ByteSize.FromBytes((ulong)properties["System.FreeSpace"]).GigaBytes);
                MaxSpace = Convert.ToUInt64(ByteSizeLib.ByteSize.FromBytes((ulong)properties["System.Capacity"]).GigaBytes);
                SpaceText = String.Format("{0} of {1}",
                    ByteSizeLib.ByteSize.FromBytes((ulong)properties["System.FreeSpace"]).ToString(),
                    ByteSizeLib.ByteSize.FromBytes((ulong)properties["System.Capacity"]).ToString());
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