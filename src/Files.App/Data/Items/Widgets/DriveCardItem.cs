// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Media.Imaging;

namespace Files.App.Data.Items
{
	public class DriveCardItem : WidgetCardItem, IWidgetCardItem<DriveItem>, IComparable<DriveCardItem>
	{
		private BitmapImage thumbnail;
		private byte[] thumbnailData;

		public new DriveItem Item { get; private set; }
		public bool HasThumbnail => thumbnail is not null && thumbnailData is not null;
		public BitmapImage Thumbnail
		{
			get => thumbnail;
			set => SetProperty(ref thumbnail, value);
		}
		public DriveCardItem(DriveItem item)
		{
			Item = item;
			Path = item.Path;
		}

		public async Task LoadCardThumbnailAsync()
		{
			// Try load thumbnail using ListView mode
			if (thumbnailData is null || thumbnailData.Length == 0)
				thumbnailData = await FileThumbnailHelper.LoadIconFromPathAsync(Item.Path, Convert.ToUInt32(Constants.Widgets.WidgetIconSize), Windows.Storage.FileProperties.ThumbnailMode.SingleItem, Windows.Storage.FileProperties.ThumbnailOptions.ResizeThumbnail);

			// Thumbnail is still null, use DriveItem icon (loaded using SingleItem mode)
			if (thumbnailData is null || thumbnailData.Length == 0)
			{
				await Item.LoadThumbnailAsync();
				thumbnailData = Item.IconData;
			}

			// Thumbnail data is valid, set the item icon
			if (thumbnailData is not null && thumbnailData.Length > 0)
				Thumbnail = await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(() => thumbnailData.ToBitmapAsync(Constants.Widgets.WidgetIconSize));
		}

		public int CompareTo(DriveCardItem? other) => Item.Path.CompareTo(other?.Item?.Path);
	}
}
