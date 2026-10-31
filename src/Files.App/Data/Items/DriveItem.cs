// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Runtime.CompilerServices;
using Windows.Storage;
using Windows.Storage.Streams;
using WinRT;
using ByteSize = ByteSizeLib.ByteSize;

namespace Files.App.Data.Items
{
	public sealed partial class DriveItem : ExpandableSidebarItemBase, INavigationControlItem, IFolder, IExpandableSidebarFolder
	{
		private BitmapImage? icon;
		public BitmapImage? Icon
		{
			get => icon;
			set
			{
				SetProperty(ref icon, value, nameof(Icon));
				OnPropertyChanged(nameof(IconElement));
			}
		}

		public byte[]? IconData { get; set; }

		private string? path;
		public string? Path
		{
			get => path;
			set => path = value;
		}

		public string? DeviceID { get; set; }

		public StorageFolder? Root { get; set; }

		public NavigationControlItemType ItemType { get; set; } = NavigationControlItemType.Drive;

		public Visibility ItemVisibility { get; set; } = Visibility.Visible;

		public bool IsRemovable
			=> Type == DriveType.Removable || Type == DriveType.CDRom;

		public bool IsNetwork
			=> Type == DriveType.Network;

		public bool IsPinned
			=> Enumerable.Contains<string?>(App.QuickAccessManager.Model.PinnedFolders, Path);

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
					if (Type != DriveType.CloudDrive)
					{
						ToolTip = GetSizeString();
					}

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
					if (Type != DriveType.CloudDrive)
					{
						ToolTip = GetSizeString();
					}

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

		private DriveType type;
		public DriveType Type
		{
			get => type;
			set
			{
				type = value;

				if (value is DriveType.Network or DriveType.CloudDrive)
					ToolTip = Text;

				OnPropertyChanged(nameof(TypeText));
			}
		}

		public string TypeText => $"DriveType{Type}".GetLocalizedResource();

		private string filesystem = string.Empty;
		public string Filesystem
		{
			get => filesystem;
			set => SetProperty(ref filesystem, value);
		}

		private string? text;
		public string? Text
		{
			get => text;
			set => SetProperty(ref text, value);
		}

		private string? spaceText;
		public string? SpaceText
		{
			get => spaceText;
			set => SetProperty(ref spaceText, value);
		}

		public SectionType Section { get; set; }

		public ContextMenuOptions? MenuOptions { get; set; }

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

		public string Id
			=> Path ?? throw new InvalidOperationException("The drive path has not been initialized.");
		public string Name => Root?.DisplayName
			?? throw new InvalidOperationException("The drive root has not been initialized.");

		public object? Children => IsExpandableFolder ? (childItems ??= []) : null;
		private BulkConcurrentObservableCollection<INavigationControlItem>? childItems;

		protected override string ExpansionPath
			=> path ?? throw new InvalidOperationException("The drive path has not been initialized.");
		protected override BulkConcurrentObservableCollection<INavigationControlItem> EnsureChildItems() => childItems ??= [];

		private object? toolTip = "";
		public object? ToolTip
		{
			get => toolTip;
			set
			{
				SetProperty(ref toolTip, value);
			}
		}

		public IconElement? IconElement
		{
			get
			{
				// CreateIconElement throws a catastrophic 0x8000FFFF on a null source, so wait for the icon to load before building one
				if (Icon is null)
					return null;

				var source = new ImageIconSource() { ImageSource = Icon };
				return source.CreateIconElement();
			}
		}

		FrameworkElement? ISidebarItemPresentationModel.IconElement => IconElement;

		public FrameworkElement? ItemDecorator
		{
			[DynamicWindowsRuntimeCast(typeof(Style))]
			get
			{
				if (!IsRemovable)
					return null; // Non-removable items don't need the eject button

				var itemDecorator = new Button()
				{
					Style = Application.Current.Resources["SidebarEjectButtonStyle"] as Style,
					Content = new ThemedIcon()
					{
						Style = Application.Current.Resources["App.ThemedIcons.Actions.Eject.12"] as Style,
						Height = 12,
						Width = 12
					}
				};

				ToolTipService.SetToolTip(itemDecorator, Strings.Eject.GetLocalizedResource());

				itemDecorator.Click += ItemDecorator_Click;

				return itemDecorator;
			}
		}

		private void ItemDecorator_Click(object sender, RoutedEventArgs e)
		{
			var drivePath = Path
				?? throw new InvalidOperationException("The drive path has not been initialized.");
			DriveHelpers.EjectDeviceAsync(drivePath);
		}

		internal void UpdateFrom(DriveItem item)
		{
			Text = item.Text;
			Root = item.Root;
			DeviceID = item.DeviceID;
			Path = item.Path;
			MenuOptions = item.MenuOptions;
			if (Type != item.Type)
			{
				Type = item.Type;
				OnPropertyChanged(nameof(Type));
				OnPropertyChanged(nameof(TypeText));
				OnPropertyChanged(nameof(IsRemovable));
				OnPropertyChanged(nameof(IsNetwork));
				OnPropertyChanged(nameof(ItemDecorator));
			}
			OnPropertyChanged(nameof(MenuOptions));

			if (!(IconData ?? []).SequenceEqual(item.IconData ?? []))
			{
				var data = item.IconData;
				IconData = data;
				_ = MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(async () =>
				{
					var image = await data.ToBitmapAsync();
					if (ReferenceEquals(IconData, data))
						Icon = image;
				});
			}
		}

		internal static DriveItem FromServer(Server.DriveItemInfo info)
		{
			var icon = info.Icon;
			var item = new DriveItem
			{
				Text = info.Name,
				Type = info.Kind,
				Path = string.IsNullOrEmpty(info.Root.Path) ? $"\\\\?\\{info.Root.Name}\\" : info.Root.Path,
				DeviceID = info.Id.TrimEnd('\\'),
				Root = info.Root,
				IconData = icon.Length == 0 ? null : icon,
			};
			item.MenuOptions = new ContextMenuOptions
			{
				IsLocationItem = true,
				ShowEjectDevice = item.IsRemovable,
				ShowShellItems = true,
				ShowProperties = true,
			};
			return item;
		}

		public static async Task<DriveItem> CreateFromPropertiesAsync(StorageFolder root, string deviceId, string label, DriveType type, IRandomAccessStream? imageStream = null)
		{
			var item = new DriveItem();

			if (imageStream is not null)
				item.IconData = await imageStream.ToByteArrayAsync();

			item.Text = type switch
			{
				DriveType.CDRom when !string.IsNullOrEmpty(label) => root.DisplayName.Replace(label.Left(32), label),
				_ => root.DisplayName
			};
			item.Type = type;
			item.MenuOptions = new ContextMenuOptions
			{
				IsLocationItem = true,
				ShowEjectDevice = item.IsRemovable,
				ShowShellItems = true,
				ShowProperties = true
			};
			item.Path = string.IsNullOrEmpty(root.Path) ? $"\\\\?\\{root.Name}\\" : root.Path;
			item.DeviceID = deviceId;
			item.Root = root;

			_ = MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(item.UpdatePropertiesAsync);

			return item;
		}


		public async Task UpdatePropertiesAsync()
		{
			try
			{
				var properties = await Server.DriveService.GetDrivePropertiesAsync(Path ?? Root?.Path ?? string.Empty, Type == DriveType.CloudDrive);
				MaxSpace = ByteSize.FromBytes(properties.Capacity);
				FreeSpace = ByteSize.FromBytes(properties.FreeSpace);
				SpaceUsed = MaxSpace - FreeSpace;
				SpaceText = properties.Available ? GetSizeString() : Strings.Unknown.GetLocalizedResource();
				PercentageUsed = MaxSpace.Bytes > 0 ? 100.0f - (float)(FreeSpace.Bytes / MaxSpace.Bytes) * 100.0f : 0;
				Filesystem = properties.FileSystem;

				OnPropertyChanged(nameof(ShowDriveDetails));
			}
			catch (Exception)
			{
				SpaceText = Strings.Unknown.GetLocalizedResource();
				MaxSpace = SpaceUsed = FreeSpace = ByteSize.FromBytes(0);
				Filesystem = string.Empty;

				OnPropertyChanged(nameof(ShowDriveDetails));
			}
		}

		public async IAsyncEnumerable<IStorableChild> GetItemsAsync(StorableType storableType = StorableType.All, [EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			await Task.CompletedTask;
			yield break;
		}

		public int CompareTo(INavigationControlItem? other)
		{
			ArgumentNullException.ThrowIfNull(other);
			var text = Text ?? throw new InvalidOperationException("The drive name has not been initialized.");
			var otherText = other.Text
				?? throw new ArgumentException("The compared item must have a name.", nameof(other));

			var result = Type.CompareTo((other as DriveItem)?.Type ?? Type);
			return result == 0 ? text.CompareTo(otherText) : result;
		}

		public async Task LoadThumbnailAsync()
		{
			if (!string.IsNullOrEmpty(DeviceID) && !string.Equals(DeviceID, "network-folder"))
			{
				var result = await FileThumbnailHelper.GetIconAsync(
					DeviceID,
					Constants.ShellIconSizes.Small,
					false,
					IconOptions.ReturnIconOnly);

				IconData ??= result;
			}

			if (Root is not null)
			{
				using var thumbnail = await DriveHelpers.GetThumbnailAsync(Root);
				IconData ??= thumbnail is not null ? await thumbnail.ToByteArrayAsync() : null;
			}

			if (string.Equals(DeviceID, "network-folder"))
				IconData ??= UIHelpers.GetSidebarIconResourceInfo(Constants.ImageRes.Network)?.IconData;

			IconData ??= UIHelpers.GetSidebarIconResourceInfo(Constants.ImageRes.Folder)?.IconData;

			Icon ??= IconData is not null ? await IconData.ToBitmapAsync() : null;
		}

		private string GetSizeString()
		{
			return string.Format(
				Strings.DriveFreeSpaceAndCapacity.GetLocalizedResource(),
				FreeSpace.ToSizeString(),
				MaxSpace.ToSizeString());
		}
	}
}
