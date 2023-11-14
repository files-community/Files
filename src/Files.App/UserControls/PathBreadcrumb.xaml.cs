// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Files.App.UserControls
{
	/// <summary>
	/// Represents control for breadcrumbs of item path.
	/// </summary>
	public sealed partial class PathBreadcrumb : UserControl
	{
		private PathBreadcrumbViewModel ViewModel { get; } = Ioc.Default.GetRequiredService<PathBreadcrumbViewModel>();

		public PathBreadcrumb()
		{
			InitializeComponent();
		}

		private void PathBreadcrumbItemChevron_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
		{
			ViewModel.PathItemSeparator_DataContextChanged(sender, args);
		}

		private void PathBreadcrumbItemFlyout_Opened(object sender, object e)
		{
			ViewModel.PathBreadcrumbItemFlyout_Opened(sender, e);
		}

		private void PathBreadcrumbItem_DragLeave(object sender, DragEventArgs e)
		{
			ViewModel.PathBreadcrumbItem_DragLeave(sender, e);
		}

		private async void PathBreadcrumbItem_DragOver(object sender, DragEventArgs e)
		{
			await ViewModel.PathBreadcrumbItem_DragOver(sender, e);
		}

		private async void PathBreadcrumbItem_Drop(object sender, DragEventArgs e)
		{
			await ViewModel.PathBreadcrumbItem_Drop(sender, e);
		}

		private async void PathBreadcrumbItem_Tapped(object sender, TappedRoutedEventArgs e)
		{
			await ViewModel.PathBreadcrumbItem_Tapped(sender, e);
		}

		private void PathBreadcrumbItem_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			ViewModel.PathBreadcrumbItem_PointerPressed(sender, e);
		}

		private void PathBreadcrumbItemChevron_Loaded(object sender, RoutedEventArgs e)
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
