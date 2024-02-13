// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.ViewModels.UserControls.Widgets;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using Windows.UI.Core;

namespace Files.App.UserControls.Widgets
{
	/// <summary>
	/// Represents group of control displays a list of quick access folders with <see cref="WidgetFolderCardItem"/>.
	/// </summary>
	public sealed partial class QuickAccessWidget : UserControl
	{
		private QuickAccessWidgetViewModel ViewModel { get; } = new();

		public QuickAccessWidget()
		{
			InitializeComponent();
		}

		public void Button_RightTapped(object sender, RightTappedRoutedEventArgs e)
		{
			ViewModel.BuildContextFlyout(sender, e);
		}

		private async void Button_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not Button button || button.Tag.ToString() is not string path || string.IsNullOrEmpty(path))
				return;

			await ViewModel.OpenFileLocation(path);
		}

		private async void Button_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			if (e.GetCurrentPoint(null).Properties.IsMiddleButtonPressed)
			{
				string path = ((Button)sender).Tag.ToString()!;
				await NavigationHelpers.OpenPathInNewTab(path);
			}
		}

		private void MenuFlyout_Opening(object sender)
		{
			if (sender is not MenuFlyout menuFlyout ||
				menuFlyout.Items.SingleOrDefault(x => x.Name == "PinFromFavorites") is not MenuFlyoutItemBase pinToFavoritesItem ||
				pinToFavoritesItem.DataContext is not DriveItem driveItemToPin)
				return;

			pinToFavoritesItem.Visibility = driveItemToPin.IsPinned ? Visibility.Collapsed : Visibility.Visible;

			if (menuFlyout.Items.SingleOrDefault(x => x.Name == "UnpinFromFavorites") is not MenuFlyoutItemBase unpinFromFavoritesItem ||
				pinToFavoritesItem.DataContext is not DriveItem driveItemToUnpin)
				return;

			unpinFromFavoritesItem.Visibility = driveItemToUnpin.IsPinned ? Visibility.Visible : Visibility.Collapsed;
		}
	}
}
