// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage.FileProperties;

namespace Files.App.Data.Items
{
	public sealed class WidgetDriveCardItem : WidgetCardItem, IWidgetCardItem<DriveItem>, IComparable<WidgetDriveCardItem>
	{
		private byte[] thumbnailData;

		public new DriveItem Item { get; private set; }

		private BitmapImage thumbnail;
		public BitmapImage Thumbnail
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
			var result = await FileThumbnailHelper.GetIconAsync(
				Item.Path,
				Constants.ShellIconSizes.Large,
				true,
				IconOptions.ReturnIconOnly | IconOptions.UseCurrentScale);

			if (result is null)
			{
				result ??= await FileThumbnailHelper.GetIconAsync(Item.Root, 40, ThumbnailMode.SingleItem, ThumbnailOptions.UseCurrentScale);
			}

			thumbnailData = result;

			var bitmapImage = await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(() => thumbnailData.ToBitmapAsync(), Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal);
			if (bitmapImage is not null)
				Thumbnail = bitmapImage;
		}

		public int CompareTo(WidgetDriveCardItem? other)
			=> Item.Path.CompareTo(other?.Item?.Path);
	}
}
