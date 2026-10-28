// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Win32;
using Windows.Win32.System.Com;
using Windows.Win32.System.WinRT;
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
		public BitmapImage? Thumbnail { get => _Thumbnail; set => SetProperty(ref _Thumbnail, value); }
		private bool _isDisposed;

		// Constructor

		public WidgetFolderCardItem(IWindowsStorable item, string text, string path, bool isPinned, string tooltip)
		{
			AutomationProperties = text;
			Item = item;
			Text = text;
			IsPinned = isPinned;
			Path = path;
			Tooltip = tooltip;
		}

		// Methods

		public async Task LoadCardThumbnailAsync()
		{
			if (_isDisposed || string.IsNullOrEmpty(Path))
				return;

			var thumbnailSize = (int)(Constants.ShellIconSizes.Large * App.AppModel.AppWindowDPI);
			// Ensure thumbnail size is at least 1 to prevent layout errors
			thumbnailSize = Math.Max(1, thumbnailSize);
			var hr = PInvoke.RoGetAgileReference(AgileReferenceOptions.AGILEREFERENCE_DEFAULT, typeof(IShellItem).GUID, Item.ThisPtr, out IAgileReference shellItemReference);
			if (hr.ThrowIfFailedOnDebug().Failed)
				return;

			var rawThumbnailData = await STATask.RunPooled(() =>
			{
				if (shellItemReference.Resolve(out IShellItem shellItem).ThrowIfFailedOnDebug().Failed)
					return null;

				using var folder = new WindowsFolder(shellItem);
				folder.TryGetThumbnail(thumbnailSize, SIIGBF.SIIGBF_ICONONLY, out var data);
				return data;
			}, App.Logger);
			if (_isDisposed || rawThumbnailData is null)
				return;

			var thumbnail = await rawThumbnailData.ToBitmapAsync();
			if (!_isDisposed)
				Thumbnail = thumbnail;
		}

		public void Dispose()
		{
			_isDisposed = true;
			Item.Dispose();
		}
	}
}
