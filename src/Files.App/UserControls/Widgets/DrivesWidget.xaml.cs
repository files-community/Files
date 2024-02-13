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
	/// Represents group of control displays a list of <see cref="WidgetDriveCardItem"/>.
	/// </summary>
	public sealed partial class DrivesWidget : UserControl
	{
		private DrivesWidgetViewModel ViewModel = new();

		public DrivesWidget()
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

			if (await DriveHelpers.CheckEmptyDrive(path))
				return;

			var ctrlPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
			if (ctrlPressed)
			{
				await NavigationHelpers.OpenPathInNewTab(path);
				return;
			}

			DrivesWidgetInvoked?.Invoke(this, new() { Path = path });
		}

		private async void Button_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			if (!e.GetCurrentPoint(null).Properties.IsMiddleButtonPressed)
				return;

			if (sender is not Button button || button.Tag.ToString() is not string path || string.IsNullOrEmpty(path))
				return;

			if (await DriveHelpers.CheckEmptyDrive(path))
				return;

			await NavigationHelpers.OpenPathInNewTab(path);
		}

		private async void GoToStorageSense_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not Button button || button.Tag.ToString() is not string path || string.IsNullOrEmpty(path))
				return;

			await StorageSenseHelper.OpenStorageSenseAsync(path);
		}

		// TODO: This is used?
		private void MenuFlyout_Opening(object sender, object e)
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
