using ByteSizeLib;
using Files.Common;
using Files.Extensions;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Uwp.Extensions;
using Microsoft.Toolkit.Uwp.Helpers;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Xaml;

namespace Files.Filesystem
{
    public class DriveItem : ObservableObject, INavigationControlItem
    {
        public string Glyph { get; set; }

        private string path;

        public string Path
        {
            get => path;
            set
            {
                path = value;
                HoverDisplayText = Path.Contains("?") ? Text : Path;
            }
        }

        public string HoverDisplayText { get; private set; }
        public string DeviceID { get; set; }
        public StorageFolder Root { get; set; }
        public NavigationControlItemType ItemType { get; set; } = NavigationControlItemType.Drive;
        public Visibility ItemVisibility { get; set; } = Visibility.Visible;

        public bool IsRemovable => Type == DriveType.Removable || Type == DriveType.CDRom;
        public bool IsNetwork => Type == DriveType.Network;

        private ByteSize maxSpace;
        private ByteSize freeSpace;
        private ByteSize spaceUsed;

        public ByteSize MaxSpace
        {
            get => maxSpace;
            set => SetProperty(ref maxSpace, value);
        }

        public ByteSize FreeSpace
        {
            get => freeSpace;
            set => SetProperty(ref freeSpace, value);
        }

        public ByteSize SpaceUsed
        {
            get => spaceUsed;
            set => SetProperty(ref spaceUsed, value);
        }

        public Visibility ShowDriveDetails
        {
            get => MaxSpace.Bytes > 0d ? Visibility.Visible : Visibility.Collapsed;
        }

        private DriveType type;

        public DriveType Type
        {
            get => type;
            set
            {
                type = value;
                SetGlyph(type);
            }
        }

        private string text;

        public string Text
        {
            get => text;
            set => SetProperty(ref text, value);
        }

        private string spaceText;

        public string SpaceText
        {
            get => spaceText;
            set => SetProperty(ref spaceText, value);
        }

        private Color progressColor = Color.FromArgb(255, 41, 132, 204);

        public Color ProgressColor
        {
	        get => progressColor;
	        set => SetProperty(ref progressColor, value);
        }

        public DriveItem()
        {
            ItemType = NavigationControlItemType.CloudDrive;
        }

        public DriveItem(StorageFolder root, string deviceId, DriveType type)
        {
            Text = root.DisplayName;
            Type = type;
            Path = string.IsNullOrEmpty(root.Path) ? $"\\\\?\\{root.Name}\\" : root.Path;
            DeviceID = deviceId;
            Root = root;

            CoreApplication.MainView.ExecuteOnUIThreadAsync(() => UpdatePropertiesAsync());
        }

        public async Task UpdateLabelAsync()
        {
            try
            {
                var properties = await Root.Properties.RetrievePropertiesAsync(new[] { "System.ItemNameDisplay" })
                    .AsTask().WithTimeoutAsync(TimeSpan.FromSeconds(5));
                Text = (string)properties["System.ItemNameDisplay"];
            }
            catch (NullReferenceException)
            {
            }
        }

        public async Task UpdatePropertiesAsync()
        {
            try
            {
                var properties = await Root.Properties.RetrievePropertiesAsync(new[] { "System.FreeSpace", "System.Capacity" })
                    .AsTask().WithTimeoutAsync(TimeSpan.FromSeconds(5));

                if (properties["System.Capacity"] != null && properties["System.FreeSpace"] != null)
                {
	                ulong maxSpace = (ulong) properties["System.Capacity"];
	                ulong freeSpace = (ulong) properties["System.FreeSpace"];

                    MaxSpace = ByteSize.FromBytes(maxSpace);
                    FreeSpace = ByteSize.FromBytes(freeSpace);
                    SpaceUsed = MaxSpace - FreeSpace;

                    SpaceText = string.Format(
                        "DriveFreeSpaceAndCapacity".GetLocalized(),
                        FreeSpace.ToBinaryString().ConvertSizeAbbreviation(),
                        MaxSpace.ToBinaryString().ConvertSizeAbbreviation());

                    if (freeSpace < maxSpace * 0.1)
                    {
	                    ProgressColor = Colors.Red;
                    }
                }
            }
            catch (Exception)
            {
                SpaceText = "DriveCapacityUnknown".GetLocalized();
                SpaceUsed = ByteSize.FromBytes(0);
            }
        }

        private void SetGlyph(DriveType type)
        {
            switch (type)
            {
                case DriveType.Fixed:
                    Glyph = "\xEDA2";
                    break;

                case DriveType.Removable:
                    Glyph = "\xE88E";
                    break;

                case DriveType.Network:
                    Glyph = "\xE8CE";
                    break;

                case DriveType.Ram:
                    Glyph = "\xE950";
                    break;

                case DriveType.CDRom:
                    Glyph = "\uE958";
                    break;

                case DriveType.Unknown:
                    break;

                case DriveType.NoRootDirectory:
                    Glyph = "\xED25";
                    break;

                case DriveType.VirtualDrive:
                    Glyph = "\uE753";
                    break;

                case DriveType.CloudDrive:
                    Glyph = "\uE753";
                    break;

                case DriveType.FloppyDisk:
                    Glyph = "\xE74E";
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
        VirtualDrive,
        CloudDrive,
    }
}