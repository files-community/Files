// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.ViewModels.UserControls.Widgets;
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
		private QuickAccessWidgetViewModel ViewModel = new();

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
			string ClickedCard = (sender as Button).Tag.ToString();
			string NavigationPath = ClickedCard; // path to navigate

			if (string.IsNullOrEmpty(NavigationPath))
				return;

			var ctrlPressed = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
			if (ctrlPressed)
			{
				await NavigationHelpers.OpenPathInNewTab(NavigationPath);
				return;
			}

			CardInvoked?.Invoke(this, new QuickAccessCardInvokedEventArgs { Path = NavigationPath });
		}

		private async void Button_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			if (e.GetCurrentPoint(null).Properties.IsMiddleButtonPressed) // check middle click
			{
				string navigationPath = ((Button)sender).Tag.ToString()!;
				await NavigationHelpers.OpenPathInNewTab(navigationPath);
			}
		}

		private void MenuFlyout_Opening(object sender)
		{
			var pinToFavoritesItem = (sender as MenuFlyout)?.Items.SingleOrDefault(x => x.Name == "PinToFavorites");
			if (pinToFavoritesItem is not null)
				pinToFavoritesItem.Visibility = (pinToFavoritesItem.DataContext as WidgetFolderCardItem)?.IsPinned ?? false ? Visibility.Collapsed : Visibility.Visible;

			var unpinFromFavoritesItem = (sender as MenuFlyout)?.Items.SingleOrDefault(x => x.Name == "UnpinFromFavorites");
			if (unpinFromFavoritesItem is not null)
				unpinFromFavoritesItem.Visibility = (unpinFromFavoritesItem.DataContext as WidgetFolderCardItem)?.IsPinned ?? false ? Visibility.Visible : Visibility.Collapsed;
		}
	}
}
