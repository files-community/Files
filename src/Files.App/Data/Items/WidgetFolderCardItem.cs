// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Win32;
using Windows.Win32.UI.Shell;

namespace Files.App.Data.Items
{
	public sealed partial class WidgetFolderCardItem : WidgetCardItem, IWidgetCardItem<IWindowsStorable>, IDisposable
	{
		// Properties

		public string? AutomationProperties { get; set; }

		public new IWindowsStorable Item { get; private set; }

		public string? Text { get; set; }

		public bool IsPinned { get; set; }

		public string Tooltip { get; set; }

		private BitmapImage? _Thumbnail;
		private bool _isThumbnailLoaded = false;

		public BitmapImage? Thumbnail
		{
			get
			{
				if (!_isThumbnailLoaded)
				{
					_ = LoadCardThumbnailAsync(); // Fire and forget
				}
				return _Thumbnail;
			}
			set => SetProperty(ref _Thumbnail, value);
		}

		// Constructor

		public WidgetFolderCardItem(IWindowsStorable item, string text, bool isPinned, string tooltip)
		{
			AutomationProperties = text;
			Item = item;
			Text = text;
			IsPinned = isPinned;
			Path = item.GetDisplayName(SIGDN.SIGDN_DESKTOPABSOLUTEPARSING);
			Tooltip = tooltip;
		}

		// Methods

		private async Task LoadCardThumbnailAsync()
		{
			if (_isThumbnailLoaded || string.IsNullOrEmpty(Path))
				return;

			Item.TryGetThumbnail((int)(Constants.ShellIconSizes.Large * App.AppModel.AppWindowDPI), SIIGBF.SIIGBF_ICONONLY, out var rawThumbnailData);
			if (rawThumbnailData is null)
				return;

			Thumbnail = await rawThumbnailData.ToBitmapAsync();
			_isThumbnailLoaded = true;
		}

		public void Dispose()
		{
			Item.Dispose();
		}
	}
}
