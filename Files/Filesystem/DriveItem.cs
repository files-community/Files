using System;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml;

namespace Files.Filesystem
{
    public class DriveItem : INavigationControlItem
    {
        public string Glyph { get; set; }
        public ulong MaxSpace { get; set; } = 0;
        public ulong SpaceUsed { get; set; } = 0;
        public string DriveText { get; set; }
        public string Tag { get; set; }
        public Visibility ProgressBarVisibility { get; set; }
        public string SpaceText { get; set; }
        public Visibility CloudGlyphVisibility { get; set; } = Visibility.Collapsed;
        public Visibility DriveGlyphVisibility { get; set; } = Visibility.Visible;
        public Visibility ItemVisibility { get; set; } = Visibility.Visible;
        public DriveType Type { get; set; }
        string INavigationControlItem.IconGlyph => Glyph;
        string INavigationControlItem.Text => DriveText;
        string INavigationControlItem.Path => Tag;
        private readonly NavigationControlItemType NavItemType = NavigationControlItemType.Drive;
        NavigationControlItemType INavigationControlItem.ItemType => NavItemType;

        public DriveItem()
        {
            NavItemType = NavigationControlItemType.OneDrive;
        }

        public DriveItem(StorageFolder root, Visibility progressBarVisibility, DriveType type)
        {
            this.ProgressBarVisibility = progressBarVisibility;
            Type = type;

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

            DriveText = root.DisplayName;

            Tag = root.Path;

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