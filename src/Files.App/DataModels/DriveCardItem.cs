using CommunityToolkit.WinUI;
using Files.App.DataModels.NavigationControlItems;
using Files.App.Helpers;
using Files.App.ViewModels.Widgets;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Threading.Tasks;

namespace Files.App.DataModels
{
	public class DriveCardItem : WidgetCardItem, IWidgetCardItem<DriveItem>, IComparable<DriveCardItem>
	{
		private byte[] thumbnailData;

		public new DriveItem Item { get; private set; }

		public bool HasThumbnail
			=> thumbnail is not null && thumbnailData is not null;

		private BitmapImage thumbnail;
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
				thumbnailData = await FileThumbnailHelper.LoadIconFromPathAsync(Item.Path, Convert.ToUInt32(Constants.Widgets.WidgetIconSize), Windows.Storage.FileProperties.ThumbnailMode.SingleItem);

			// Thumbnail is still null, use DriveItem icon (loaded using SingleItem mode)
			if (thumbnailData is null || thumbnailData.Length == 0)
				thumbnailData = Item.IconData;

			// Thumbnail data is valid, set the item icon
			if (thumbnailData is not null && thumbnailData.Length > 0)
				Thumbnail = await App.Window.DispatcherQueue.EnqueueAsync(() => thumbnailData.ToBitmapAsync(Constants.Widgets.WidgetIconSize));
		}

		public int CompareTo(DriveCardItem? other)
			=> Item.Path.CompareTo(other?.Item?.Path);
	}
}
