// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Files.App.UserControls
{
	public sealed partial class PathBreadcrumb : UserControl
	{
		[GeneratedDependencyProperty]
		public partial NavigationToolbarViewModel ViewModel { get; set; }

		public PathBreadcrumb()
		{
			InitializeComponent();
		}

		private void PathItemSeparator_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
		{
			ViewModel.PathItemSeparator_DataContextChanged(sender, args);
		}

		private void PathBoxItemFlyout_Opening(object sender, object e)
		{
			ViewModel.PathboxItemFlyout_Opening(sender, e);
		}

		private void PathBoxItemFlyout_Closed(object sender, object e)
		{
			ViewModel.PathBoxItemFlyout_Closed(sender, e) ;
		}

		private void PathBoxItem_DragLeave(object sender, DragEventArgs e)
		{
			ViewModel.PathBoxItem_DragLeave(sender, e);
		}

		private async void PathBoxItem_DragOver(object sender, DragEventArgs e)
		{
			await ViewModel.PathBoxItem_DragOver(sender, e);
		}

		private async void PathBoxItem_Drop(object sender, DragEventArgs e)
		{
			await ViewModel.PathBoxItem_Drop(sender, e);
		}

		private async void PathBoxItem_Tapped(object sender, TappedRoutedEventArgs e)
		{
			if (sender is not TextBlock textBlock ||
				textBlock.DataContext is not PathBoxItem item ||
				item.Path is not { } path)
				return;

			// TODO: Implement middle click retrieving.
			await ViewModel.HandleFolderNavigationAsync(path);

			e.Handled = true;
		}

		private void PathBoxItem_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			ViewModel.PathBoxItem_PointerPressed(sender, e);
		}

		private void PathBoxItem_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
		{
			ViewModel.PathBoxItem_PreviewKeyDown(sender, e);
		}
	}
}
