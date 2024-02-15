// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Media.Imaging;

namespace Files.App.Data.Items
{
	public class WidgetFolderCardItem : WidgetCardItem, IWidgetCardItem<LocationItem>
	{
		// Fields

		private byte[] _thumbnailData;

		// Properties

		public string? AutomationProperties { get; set; }

		public LocationItem? Item { get; private set; }

		public string? Text { get; set; }

		public bool IsPinned { get; set; }

		public bool HasPath
			=> !string.IsNullOrEmpty(Path);

		private BitmapImage? _Thumbnail;
		public BitmapImage? Thumbnail
		{
			get => _Thumbnail;
			set => SetProperty(ref _Thumbnail, value);
		}

		// Constructor

		public WidgetFolderCardItem(LocationItem item, string text, bool isPinned)
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

		// Methods

		public async Task LoadCardThumbnailAsync()
		{
			_thumbnailData = await FileThumbnailHelper.LoadIconWithoutOverlayAsync(Path, Constants.ShellIconSizes.Jumbo, true, false, true);

			if (_thumbnailData is not null && _thumbnailData.Length > 0)
				Thumbnail = await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(() => _thumbnailData.ToBitmapAsync(), Microsoft.UI.Dispatching.DispatcherQueuePriority.Low);
		}
	}
}
