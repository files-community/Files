// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Files.App.UserControls
{
	[DependencyProperty<ToolbarViewModel>("ViewModel")]
	public sealed partial class PathBreadcrumb : UserControl
	{
		public PathBreadcrumb()
		{
			InitializeComponent();
		}

		private void PathItemSeparator_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
		{
			ViewModel.PathItemSeparator_DataContextChanged(sender, args);
		}

		private void PathBoxItemFlyout_Opened(object sender, object e)
		{
			ViewModel.PathboxItemFlyout_Opened(sender, e);
		}

		private void PathBoxItem_DragLeave(object sender, DragEventArgs e)
		{
			ViewModel.PathBoxItem_DragLeave(sender, e);
		}

		private async void PathBoxItem_DragOverAsync(object sender, DragEventArgs e)
		{
			await ViewModel.PathBoxItem_DragOverAsync(sender, e);
		}

		private async void PathBoxItem_DropAsync(object sender, DragEventArgs e)
		{
			await ViewModel.PathBoxItem_DropAsync(sender, e);
		}

		private async void PathBoxItem_TappedAsync(object sender, TappedRoutedEventArgs e)
		{
			await ViewModel.PathBoxItem_TappedAsync(sender, e);
		}

		private void PathBoxItem_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			ViewModel.PathBoxItem_PointerPressed(sender, e);
		}

		private void PathItemSeparator_Loaded(object sender, RoutedEventArgs e)
		{
			if (sender is not FontIcon pathSeparatorIcon)
				return;

			pathSeparatorIcon.Tapped += (s, e) =>
			{
				pathSeparatorIcon.ContextFlyout.ShowAt(pathSeparatorIcon);
			};

			pathSeparatorIcon.ContextFlyout.Opened += (s, e) =>
			{
				pathSeparatorIcon.Glyph = "\uE70D";
			};

			pathSeparatorIcon.ContextFlyout.Closed += (s, e) =>
			{
				pathSeparatorIcon.Glyph = "\uE76C";
			};
		}
	}
}
