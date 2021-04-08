using ByteSizeLib;
using Files.Common;
using Files.Extensions;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Uwp;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;

namespace Files.Filesystem
{
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

    public class DriveItem : ObservableObject, INavigationControlItem
    {
        private ByteSize freeSpace;
        private ByteSize maxSpace;
        private string path;
        private float percentageUsed = 0.0f;
        private bool showStorageSense = false;
        private string spaceText;
        private ByteSize spaceUsed;
        private string text;
        private DriveType type;

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

            CoreApplication.MainView.DispatcherQueue.EnqueueAsync(() => UpdatePropertiesAsync());
        }

        public string DeviceID { get; set; }

        public ByteSize FreeSpace
        {
            get => freeSpace;
            set => SetProperty(ref freeSpace, value);
        }

        public string HoverDisplayText { get; private set; }
        public SvgImageSource Icon { get; set; }
        public bool IsNetwork => Type == DriveType.Network;

        public bool IsRemovable => Type == DriveType.Removable || Type == DriveType.CDRom;

        public NavigationControlItemType ItemType { get; set; } = NavigationControlItemType.Drive;

        public Visibility ItemVisibility { get; set; } = Visibility.Visible;

        public ByteSize MaxSpace
        {
            get => maxSpace;
            set => SetProperty(ref maxSpace, value);
        }

        public string Path
        {
            get => path;
            set
            {
                path = value;
                HoverDisplayText = Path.Contains("?") ? Text : Path;
            }
        }

        public float PercentageUsed
        {
            get => percentageUsed;
            set
            {
                if (SetProperty(ref percentageUsed, value))
                {
                    if (Type == DriveType.Fixed)
                    {
                        if (percentageUsed >= Constants.Widgets.Drives.LowStorageSpacePercentageThreshold)
                        {
                            ShowStorageSense = true;
                        }
                        else
                        {
                            ShowStorageSense = false;
                        }
                    }
                }
            }
        }

        public StorageFolder Root { get; set; }
        public SectionType Section { get; set; }

        public Visibility ShowDriveDetails
        {
            get => MaxSpace.Bytes > 0d ? Visibility.Visible : Visibility.Collapsed;
        }

        public bool ShowStorageSense
        {
            get => showStorageSense;
            set => SetProperty(ref showStorageSense, value);
        }

        public string SpaceText
        {
            get => spaceText;
            set => SetProperty(ref spaceText, value);
        }

        public ByteSize SpaceUsed
        {
            get => spaceUsed;
            set => SetProperty(ref spaceUsed, value);
        }

        public string Text
        {
            get => text;
            set => SetProperty(ref text, value);
        }

        public DriveType Type
        {
            get => type;
            set
            {
                type = value;
                SetGlyph(type);
            }
        }

        public int CompareTo(INavigationControlItem other)
        {
            var result = Type.CompareTo((other as DriveItem)?.Type ?? Type);
            if (result == 0)
            {
                return Text.CompareTo(other.Text);
            }
            return result;
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
                    MaxSpace = ByteSize.FromBytes((ulong)properties["System.Capacity"]);
                    FreeSpace = ByteSize.FromBytes((ulong)properties["System.FreeSpace"]);
                    SpaceUsed = MaxSpace - FreeSpace;

                    SpaceText = string.Format(
                        "DriveFreeSpaceAndCapacity".GetLocalized(),
                        FreeSpace.ToBinaryString().ConvertSizeAbbreviation(),
                        MaxSpace.ToBinaryString().ConvertSizeAbbreviation());

                    if (FreeSpace.Bytes > 0 && MaxSpace.Bytes > 0) // Make sure we don't divide by 0
                    {
                        PercentageUsed = 100.0f - ((float)(FreeSpace.Bytes / MaxSpace.Bytes) * 100.0f);
                    }
                }
            }
            catch (Exception)
            {
                SpaceText = "DriveCapacityUnknown".GetLocalized();
                SpaceUsed = ByteSize.FromBytes(0);
            }
        }

        private async void SetGlyph(DriveType type)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                switch (type)
                {
                    case DriveType.Fixed:
                        Icon = new SvgImageSource(new Uri("ms-appx:///Assets/FluentIcons/Drive.svg"));
                        break;

                    case DriveType.Removable:
                        Icon = new SvgImageSource(new Uri("ms-appx:///Assets/FluentIcons/Folder.svg")); // TODO
                        break;

                    case DriveType.Network:
                        Icon = new SvgImageSource(new Uri("ms-appx:///Assets/FluentIcons/Drive_Network.svg"));
                        break;

                    case DriveType.Ram:
                        Icon = new SvgImageSource(new Uri("ms-appx:///Assets/FluentIcons/Folder.svg")); // TODO
                        break;

                    case DriveType.CDRom:
                        Icon = new SvgImageSource(new Uri("ms-appx:///Assets/FluentIcons/Folder.svg")); // TODO
                        break;

                    case DriveType.Unknown:
                        break;

                    case DriveType.NoRootDirectory:
                        Icon = new SvgImageSource(new Uri("ms-appx:///Assets/FluentIcons/Folder.svg")); // TODO
                        break;

                    case DriveType.VirtualDrive:
                        Icon = new SvgImageSource(new Uri("ms-appx:///Assets/FluentIcons/Folder.svg")); // TODO
                        break;

                    case DriveType.CloudDrive:
                        Icon = new SvgImageSource(new Uri("ms-appx:///Assets/FluentIcons/Folder.svg")); // TODO
                        break;

                    case DriveType.FloppyDisk:
                        Icon = new SvgImageSource(new Uri("ms-appx:///Assets/FluentIcons/Folder.svg")); // TODO
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(type), type, null);
                }
            });
        }
    }
}