// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Media.Imaging;

namespace Files.App.Data.Items
{
	public class WidgetDriveCardItem : WidgetCardItem, IWidgetCardItem<DriveItem>, IComparable<WidgetDriveCardItem>
	{
		private byte[]? _thumbnailData;

		public new DriveItem Item { get; private set; }

		public bool HasThumbnail
			=> _Thumbnail is not null && _thumbnailData is not null;

		private BitmapImage? _Thumbnail;
		public BitmapImage? Thumbnail
		{
			get => _Thumbnail;
			set => SetProperty(ref _Thumbnail, value);
		}

		public WidgetDriveCardItem(DriveItem item)
		{
			Item = item;
			Path = item.Path;
		}

		public async Task LoadCardThumbnailAsync()
		{
			// Try load thumbnail using ListView mode
			if (_thumbnailData is null || _thumbnailData.Length == 0)
				_thumbnailData = await FileThumbnailHelper.LoadIconFromPathAsync(Item.Path, Convert.ToUInt32(Constants.Widgets.WidgetIconSize), Windows.Storage.FileProperties.ThumbnailMode.SingleItem, Windows.Storage.FileProperties.ThumbnailOptions.ResizeThumbnail);

			// Thumbnail is still null, use DriveItem icon (loaded using SingleItem mode)
			if (_thumbnailData is null || _thumbnailData.Length == 0)
			{
				await Item.LoadThumbnailAsync();
				_thumbnailData = Item.IconData;
			}

			// Thumbnail data is valid, set the item icon
			if (_thumbnailData is not null && _thumbnailData.Length > 0)
				Thumbnail = await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(() => _thumbnailData.ToBitmapAsync(Constants.Widgets.WidgetIconSize));
		}

		public int CompareTo(WidgetDriveCardItem? other)
			=> Item.Path.CompareTo(other?.Item?.Path);
	}
}
