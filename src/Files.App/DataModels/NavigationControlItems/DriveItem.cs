using ByteSizeLib;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI;
using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.Helpers;
using Files.Shared.Extensions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Files.App.DataModels.NavigationControlItems
{
	public class DriveItem : ObservableObject, INavigationControlItem
	{
		private BitmapImage icon;
		public BitmapImage Icon
		{
			get => icon;
			set => SetProperty(ref icon, value);
		}

		//public Uri IconSource { get; set; }

		public byte[] IconData { get; set; }

		private string path;
		public string Path
		{
			get => path;
			set => path = value;
		}

		public string ToolTipText { get; private set; }

		public string DeviceID { get; set; }

		public StorageFolder Root { get; set; }

		public NavigationControlItemType ItemType { get; set; } = NavigationControlItemType.Drive;

		public Visibility ItemVisibility { get; set; } = Visibility.Visible;

		public bool IsRemovable
			=> Type == DriveType.Removable || Type == DriveType.CDRom;

		public bool IsNetwork
			=> Type == DriveType.Network;

		public bool IsPinned
			=> App.QuickAccessManager.Model.FavoriteItems.Contains(path);

		public string MaxSpaceText
			=> MaxSpace.ToSizeString();

		public string FreeSpaceText
			=> FreeSpace.ToSizeString();

		public string UsedSpaceText
			=> SpaceUsed.ToSizeString();

		private ByteSize maxSpace;
		public ByteSize MaxSpace
		{
			get => maxSpace;
			set
			{
				if (SetProperty(ref maxSpace, value))
				{
					ToolTipText = GetSizeString();

					OnPropertyChanged(nameof(MaxSpaceText));
					OnPropertyChanged(nameof(ShowDriveDetails));
				}
			}
		}

		private ByteSize freeSpace;
		public ByteSize FreeSpace
		{
			get => freeSpace;
			set
			{
				if (SetProperty(ref freeSpace, value))
				{
					ToolTipText = GetSizeString();

					OnPropertyChanged(nameof(FreeSpaceText));
				}
			}
		}

		private ByteSize spaceUsed;
		public ByteSize SpaceUsed
		{
			get => spaceUsed;
			set
			{
				if (SetProperty(ref spaceUsed, value))
				{
					OnPropertyChanged(nameof(UsedSpaceText));
				}
			}
		}

		public bool ShowDriveDetails
			=> MaxSpace.Bytes > 0d;

		public DriveType Type { get; set; }

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

		public ContextMenuOptions MenuOptions { get; set; }

		private float percentageUsed = 0.0f;
		public float PercentageUsed
		{
			get => percentageUsed;
			set
			{
				if (!SetProperty(ref percentageUsed, value))
					return;

				if (Type == DriveType.Fixed)
					ShowStorageSense = percentageUsed >= Constants.Widgets.Drives.LowStorageSpacePercentageThreshold;
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

			if (imageStream is not null)
				item.IconData = await imageStream.ToByteArrayAsync();

			item.Text = type is DriveType.Network ? $"{root.DisplayName} ({deviceId})" : root.DisplayName;
			item.Type = type;
			item.MenuOptions = new ContextMenuOptions
			{
				IsLocationItem = true,
				ShowEjectDevice = item.IsRemovable,
				ShowShellItems = true,
				ShowFormatDrive = !(item.Type == DriveType.Network || string.Equals(root.Path, "C:\\", StringComparison.OrdinalIgnoreCase)),
				ShowProperties = true
			};
			item.Path = string.IsNullOrEmpty(root.Path) ? $"\\\\?\\{root.Name}\\" : root.Path;
			item.DeviceID = deviceId;
			item.Root = root;

			_ = App.Window.DispatcherQueue.EnqueueAsync(() => item.UpdatePropertiesAsync());

			return item;
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

				if (properties is not null && properties["System.Capacity"] is not null && properties["System.FreeSpace"] is not null)
				{
					MaxSpace = ByteSize.FromBytes((ulong)properties["System.Capacity"]);
					FreeSpace = ByteSize.FromBytes((ulong)properties["System.FreeSpace"]);
					SpaceUsed = MaxSpace - FreeSpace;

					SpaceText = GetSizeString();

					if (MaxSpace.Bytes > 0 && FreeSpace.Bytes > 0) // Make sure we don't divide by 0
						PercentageUsed = 100.0f - ((float)(FreeSpace.Bytes / MaxSpace.Bytes) * 100.0f);
				}
				else
				{
					SpaceText = "DriveCapacityUnknown".GetLocalizedResource();
					MaxSpace = SpaceUsed = FreeSpace = ByteSize.FromBytes(0);
				}

				OnPropertyChanged(nameof(ShowDriveDetails));
			}
			catch (Exception)
			{
				SpaceText = "DriveCapacityUnknown".GetLocalizedResource();
				MaxSpace = SpaceUsed = FreeSpace = ByteSize.FromBytes(0);

				OnPropertyChanged(nameof(ShowDriveDetails));
			}
		}

		public int CompareTo(INavigationControlItem other)
		{
			var result = Type.CompareTo((other as DriveItem)?.Type ?? Type);
			return result == 0 ? Text.CompareTo(other.Text) : result;
		}

		public async Task LoadDriveIcon()
		{
			if (IconData is null)
			{
				if (!string.IsNullOrEmpty(DeviceID) && !string.Equals(DeviceID, "network-folder"))
					IconData = await FileThumbnailHelper.LoadIconWithoutOverlayAsync(DeviceID, 24);

				IconData ??= UIHelpers.GetIconResourceInfo(Constants.ImageRes.Folder).IconData;
			}

			Icon = await IconData.ToBitmapAsync();
		}

		private string GetSizeString()
		{
			return string.Format(
				"DriveFreeSpaceAndCapacity".GetLocalizedResource(),
				FreeSpace.ToSizeString(),
				MaxSpace.ToSizeString());
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
