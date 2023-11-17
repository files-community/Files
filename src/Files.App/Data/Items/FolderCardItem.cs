// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Media.Imaging;

namespace Files.App.Data.Items
{
	/// <summary>
	/// Represents folder card used in the <see cref="WidgetItem"/>.
	/// </summary>
	public class FolderCardItem : WidgetCardItem<LocationItem>, IWidgetCardItem<LocationItem>
	{
		private byte[] _thumbnailData;

		public string AutomationProperties { get; set; }

		public LocationItem Item { get; private set; }

		public string Text { get; set; }

		public bool IsPinned { get; set; }

		public bool HasPath
			=> !string.IsNullOrEmpty(Path);

		public bool HasThumbnail
			=> _Thumbnail is not null && _thumbnailData is not null;

		private BitmapImage _Thumbnail;
		public BitmapImage Thumbnail
		{
			get => _Thumbnail;
			set => SetProperty(ref _Thumbnail, value);
		}

		public FolderCardItem(LocationItem item, string text, bool isPinned)
		{
			if (!string.IsNullOrWhiteSpace(text))
			{
				Text = text;
				AutomationProperties = Text;
			}

			IsPinned = isPinned;
			Item = item;
			Path = item.Path;
		}

		public async Task LoadCardThumbnailAsync()
		{
			if (_thumbnailData is null || _thumbnailData.Length == 0)
			{
				_thumbnailData = await FileThumbnailHelper.LoadIconFromPathAsync(Path, Convert.ToUInt32(Constants.Widgets.WidgetIconSize), Windows.Storage.FileProperties.ThumbnailMode.SingleItem, Windows.Storage.FileProperties.ThumbnailOptions.ResizeThumbnail);
			}

			if (_thumbnailData is not null && _thumbnailData.Length > 0)
			{
				Thumbnail = await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(() => _thumbnailData.ToBitmapAsync(Constants.Widgets.WidgetIconSize));
			}
		}
	}
}
