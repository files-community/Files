// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using Windows.UI.Core;
using WinRT;

namespace Files.App.UserControls
{
	/// <summary>
	/// Represents a horizontal bookmarks bar shown below the address bar.
	/// </summary>
	public sealed partial class BookmarksBar : UserControl
	{
		private IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();
		private IQuickAccessService QuickAccessService { get; } = Ioc.Default.GetRequiredService<IQuickAccessService>();

		[DynamicWindowsRuntimeCast(typeof(MenuFlyout))]
		private MenuFlyout? GetBookmarkFlyout() => Resources["BookmarkFlyout"] as MenuFlyout;

		public BookmarksBarViewModel ViewModel { get; }

		public BookmarksBar()
		{
			ViewModel = Ioc.Default.GetRequiredService<BookmarksBarViewModel>();
			InitializeComponent();

			ViewModel.Items.CollectionChanged += Items_CollectionChanged;
			Loaded += BookmarksBar_Loaded;

			UpdateEmptyHint();
		}

		private void BookmarksBar_Loaded(object sender, RoutedEventArgs e)
			=> _ = ViewModel.RefreshAsync();

		private void Items_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
			=> UpdateEmptyHint();

		private void UpdateEmptyHint()
		{
			if (EmptyHint is null)
				return;

			EmptyHint.Visibility = ViewModel.Items.Count is 0 ? Visibility.Visible : Visibility.Collapsed;
		}

		[DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
		private static LocationItem? GetItem(object? sender)
			=> (sender as FrameworkElement)?.DataContext as LocationItem;

		private async void Bookmark_Click(object sender, RoutedEventArgs e)
		{
			if (GetItem(sender) is not { Path: { } path })
				return;

			if (InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down))
			{
				await NavigationHelpers.OpenPathInNewTab(path);
				return;
			}

			if (ContentPageContext.ShellPage is not { } shellPage)
				return;

			shellPage.NavigateWithArguments(
				shellPage.InstanceViewModel.FolderSettings.GetLayoutType(path),
				new NavigationArguments() { NavPathParam = path });
		}

		private async void Bookmark_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			if (!e.GetCurrentPoint(null).Properties.IsMiddleButtonPressed)
				return;

			if (GetItem(sender) is { Path: { } path })
				await NavigationHelpers.OpenPathInNewTab(path);
		}

		[DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
		private void Bookmark_RightTapped(object sender, RightTappedRoutedEventArgs e)
		{
			if (sender is not FrameworkElement element || GetItem(sender) is null || GetBookmarkFlyout() is not { } flyout)
				return;

			flyout.ShowAt(element, new FlyoutShowOptions() { Position = e.GetPosition(element) });
			e.Handled = true;
		}

		private async void OpenInNewTab_Click(object sender, RoutedEventArgs e)
		{
			if (GetItem(GetBookmarkFlyout()?.Target) is { Path: { } path })
				await NavigationHelpers.OpenPathInNewTab(path);
		}

		private async void OpenInNewWindow_Click(object sender, RoutedEventArgs e)
		{
			if (GetItem(GetBookmarkFlyout()?.Target) is { Path: { } path })
				await NavigationHelpers.OpenPathInNewWindowAsync(path);
		}

		private async void Unpin_Click(object sender, RoutedEventArgs e)
		{
			if (GetItem(GetBookmarkFlyout()?.Target) is { Path: { } path })
				await QuickAccessService.UnpinFromSidebarAsync(path);
		}
	}
}
