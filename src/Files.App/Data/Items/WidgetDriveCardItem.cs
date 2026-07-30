// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Media.Imaging;

namespace Files.App.Data.Items
{
	public sealed partial class WidgetDriveCardItem : WidgetCardItem, IWidgetCardItem<DriveItem>, IComparable<WidgetDriveCardItem>
	{
		public new DriveItem Item { get; private set; }

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
			var result = await FileThumbnailHelper.GetIconAsync(
				Item.Path,
				Constants.ShellIconSizes.Large,
				true,
				IconOptions.ReturnIconOnly | IconOptions.UseCurrentScale);

			if (result is null && Item.Root is { } root)
			{
				using var thumbnail = await DriveHelpers.GetThumbnailAsync(root);
				result ??= await thumbnail.ToByteArrayAsync();
			}

			if (result is null)
				return;

			var bitmapImage = await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(() => result.ToBitmapAsync(), Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal);
			if (bitmapImage is not null)
				Thumbnail = bitmapImage;
		}

		public int CompareTo(WidgetDriveCardItem? other)
			=> Item.Path.CompareTo(other?.Item?.Path);
	}
}
