// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;

namespace Files.App.Data.Items
{
	public sealed partial class WidgetDriveCardItem : WidgetCardItem, IWidgetCardItem<IWindowsFolder>, IDisposable
	{
		// Properties

		public required new IWindowsFolder Item { get; set; }

		public required string Text { get; set; }

		public bool ShowStorageSense => UsedSize.GigaBytes / TotalSize.GigaBytes >= Constants.Widgets.Drives.LowStorageSpacePercentageThreshold;

		public bool ShowDriveUsage => TotalSize.GigaBytes > 0D;

		public ByteSizeLib.ByteSize TotalSize { get; set; } = default;

		public ByteSizeLib.ByteSize FreeSize { get; set; } = default;

		public ByteSizeLib.ByteSize UsedSize => ByteSizeLib.ByteSize.FromBytes(TotalSize.Bytes - FreeSize.Bytes);

		public string? UsageText => string.Format(Strings.DriveFreeSpaceAndCapacity.GetLocalizedResource(), FreeSize.ToSizeString(), TotalSize.ToSizeString());

		public required SystemIO.DriveType DriveType { get; set; }

		private BitmapImage? _Thumbnail;
		public BitmapImage? Thumbnail { get => _Thumbnail; set => SetProperty(ref _Thumbnail, value); }

		// Constructor

		public WidgetDriveCardItem()
		{
		}

		// Methods

		public async Task LoadCardThumbnailAsync()
		{
			if (string.IsNullOrEmpty(Path) || Item is not IWindowsStorable windowsStorable)
				return;

			HRESULT hr = windowsStorable.TryGetThumbnail((int)(Constants.ShellIconSizes.Large * App.AppModel.AppWindowDPI), SIIGBF.SIIGBF_ICONONLY, out var rawThumbnailData);
			if (hr.Failed || rawThumbnailData is null)
				return;

			Thumbnail = await rawThumbnailData.ToBitmapAsync();
		}

		// Disposer

		public void Dispose()
		{
			Item.Dispose();
		}
	}
}
