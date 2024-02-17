// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Media.Imaging;

namespace Files.App.Data.Items
{
	public class WidgetDriveCardItem :ObservableObject, IWidgetCardItem, IComparable<WidgetDriveCardItem>
	{
		// Field

		private byte[]? thumbnailData;

		//Properties

		public DriveItem Item { get; private set; }

		public string Path { get; set; }

		private BitmapImage? thumbnail;
		public BitmapImage? Thumbnail
		{
			get => thumbnail;
			set => SetProperty(ref thumbnail, value);
		}

		public WidgetDriveCardItem(DriveItem item)
		{
			Item = item;
			Path = item.Path;
		}

		public async Task LoadCardThumbnailAsync()
		{
			thumbnailData = await FileThumbnailHelper.LoadIconWithoutOverlayAsync(Item.Path, Constants.ShellIconSizes.Jumbo, true, false, true);

			if (thumbnailData is not null && thumbnailData.Length > 0)
				Thumbnail = await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(() => thumbnailData.ToBitmapAsync(), Microsoft.UI.Dispatching.DispatcherQueuePriority.Low);
		}

		public int CompareTo(WidgetDriveCardItem? other)
		{
			return Item.Path.CompareTo(other?.Item?.Path);
		}
	}
}
