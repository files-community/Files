// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Files.App.Attributes;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.App.UserControls
{
	[DependencyProperty<ToolbarViewModel>("ViewModel")]
	public sealed partial class PathBreadcrumb : UserControl
	{
		public PathBreadcrumb()
		{
			InitializeComponent();
		}

		private void PathItemSeparator_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args) => ViewModel.PathItemSeparator_DataContextChanged(sender, args);

		private void PathboxItemFlyout_Opened(object sender, object e) => ViewModel.PathboxItemFlyout_Opened(sender, e);

		private void PathBoxItem_DragLeave(object sender, DragEventArgs e) => ViewModel.PathBoxItem_DragLeave(sender, e);

		private void PathBoxItem_DragOver(object sender, DragEventArgs e) => ViewModel.PathBoxItem_DragOver(sender, e);

		private void PathBoxItem_Drop(object sender, DragEventArgs e) => ViewModel.PathBoxItem_Drop(sender, e);

		private void PathBoxItem_Tapped(object sender, TappedRoutedEventArgs e) => ViewModel.PathBoxItem_Tapped(sender, e);

		private void PathBoxItem_PointerPressed(object sender, PointerRoutedEventArgs e) => ViewModel.PathBoxItem_PointerPressed(sender, e);

		private void PathItemSeparator_Loaded(object sender, RoutedEventArgs e)
		{
			var pathSeparatorIcon = sender as FontIcon;
			pathSeparatorIcon.Tapped += (s, e) => pathSeparatorIcon.ContextFlyout.ShowAt(pathSeparatorIcon);
			pathSeparatorIcon.ContextFlyout.Opened += (s, e) => { pathSeparatorIcon.Glyph = "\uE70D"; };
			pathSeparatorIcon.ContextFlyout.Closed += (s, e) => { pathSeparatorIcon.Glyph = "\uE76C"; };
		}
	}
}