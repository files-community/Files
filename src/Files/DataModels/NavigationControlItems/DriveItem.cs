using ByteSizeLib;
using Files.Common;
using Files.Extensions;
using Files.Filesystem;
using Files.Helpers;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Uwp;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;

namespace Files.DataModels.NavigationControlItems
{
    public class DriveItem : ObservableObject, INavigationControlItem
    {
        public BitmapImage Icon { get; set; }
        public Uri IconSource { get; set; }
        public byte[] IconData { get; set; }

        private string path;

        public string Path
        {
            get => path;
            set
            {
                path = value;
                HoverDisplayText = Path.Contains("?", StringComparison.Ordinal) ? Text : Path;
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

        public SectionType Section { get; set; }

        private float percentageUsed = 0.0f;

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

        private bool showStorageSense = false;

        public bool ShowStorageSense
        {
            get => showStorageSense;
            set => SetProperty(ref showStorageSense, value);
        }

        public DriveItem()
        {
            ItemType = NavigationControlItemType.CloudDrive;
        }

        public static async Task<DriveItem> CreateFromPropertiesAsync(StorageFolder root, string deviceId, DriveType type, IRandomAccessStream imageStream = null)
        {
            var item = new DriveItem();

            await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(async () => await item.SetBitmapImage(imageStream));
            item.Text = root.DisplayName;
            item.Type = type;
            item.Path = string.IsNullOrEmpty(root.Path) ? $"\\\\?\\{root.Name}\\" : root.Path;
            item.DeviceID = deviceId;
            item.Root = root;
            _ = CoreApplication.MainView.DispatcherQueue.EnqueueAsync(() => item.UpdatePropertiesAsync());

            return item;
        }

        public async Task SetBitmapImage(IRandomAccessStream imageStream)
        {
            if (imageStream != null)
            {
                IconData = await imageStream.ToByteArrayAsync();
                Icon = await IconData.ToBitmapAsync();
            }
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

                if (properties != null && properties["System.Capacity"] != null && properties["System.FreeSpace"] != null)
                {
                    MaxSpace = ByteSize.FromBytes((ulong)properties["System.Capacity"]);
                    FreeSpace = ByteSize.FromBytes((ulong)properties["System.FreeSpace"]);
                    SpaceUsed = MaxSpace - FreeSpace;

                    SpaceText = string.Format(
                        "DriveFreeSpaceAndCapacity".GetLocalized(),
                        FreeSpace.ToSizeString(),
                        MaxSpace.ToSizeString());

                    if (FreeSpace.Bytes > 0 && MaxSpace.Bytes > 0) // Make sure we don't divide by 0
                    {
                        PercentageUsed = 100.0f - ((float)(FreeSpace.Bytes / MaxSpace.Bytes) * 100.0f);
                    }
                }
                else
                {
                    SpaceText = "DriveCapacityUnknown".GetLocalized();
                    MaxSpace = SpaceUsed = FreeSpace = ByteSize.FromBytes(0);
                }
            }
            catch (Exception)
            {
                SpaceText = "DriveCapacityUnknown".GetLocalized();
                MaxSpace = SpaceUsed = FreeSpace = ByteSize.FromBytes(0);
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