$\xEF\xBB\xBF// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;

namespace Files.App.UserControls
{
	public sealed partial class BookmarksBar : UserControl
	{
		public BookmarksBarViewModel ViewModel { get; } = Ioc.Default.GetRequiredService<BookmarksBarViewModel>();

		public BookmarksBar()
		{
			InitializeComponent();

			Loaded += async (_, _) => await ViewModel.InitializeAsync();
		}

		private async void BookmarkButton_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not Button { DataContext: BookmarkItem item } button)
				return;

			if (item.IsGroup)
				ShowGroupFlyout(button, item);
			else
				await ViewModel.OpenAsync(item);
		}

		private void ShowGroupFlyout(FrameworkElement anchor, BookmarkItem group)
		{
			var flyout = new MenuFlyout();

			foreach (var child in group.Children)
			{
				var menuItem = new MenuFlyoutItem() { Text = child.Title, Tag = child };
				menuItem.Click += async (sender, _) =>
				{
					if (sender is MenuFlyoutItem { Tag: BookmarkItem target })
						await ViewModel.OpenAsync(target);
				};

				flyout.Items.Add(menuItem);
			}

			flyout.ShowAt(anchor);
		}

		private async void RemoveBookmark_Click(object sender, RoutedEventArgs e)
		{
			if (sender is MenuFlyoutItem { Tag: BookmarkItem item })
				await ViewModel.RemoveAsync(item);
		}

		private void RootGrid_DragOver(object sender, DragEventArgs e)
		{
			e.AcceptedOperation = e.DataView.Contains(StandardDataFormats.StorageItems)
				? DataPackageOperation.Link
				: DataPackageOperation.None;
		}

		private async void RootGrid_Drop(object sender, DragEventArgs e)
		{
			if (!e.DataView.Contains(StandardDataFormats.StorageItems))
				return;

			var deferral = e.GetDeferral();

			try
			{
				foreach (var storageItem in await e.DataView.GetStorageItemsAsync())
					await ViewModel.AddPathAsync(storageItem.Path);
			}
			finally
			{
				deferral.Complete();
			}
		}
	}
}
