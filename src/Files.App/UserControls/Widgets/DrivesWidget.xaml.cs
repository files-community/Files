// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Files.App.UserControls.Widgets
{
	public sealed partial class DrivesWidget : UserControl
	{
		public DrivesWidgetViewModel ViewModel { get; } = Ioc.Default.GetRequiredService<DrivesWidgetViewModel>();

		public DrivesWidget()
		{
			InitializeComponent();
		}

		private async void Button_Click(object sender, RoutedEventArgs e)
		{
			if (sender is Button button && button.Tag is string navigationPath)
			{
				await ViewModel.Open(navigationPath);
			}
		}

		private async void Button_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			// Check if the clicking mode was middle click
			if (!e.GetCurrentPoint(null).Properties.IsMiddleButtonPressed)
				return;

			if (sender is Button button && button.Tag is string navigationPath)
			{
				if (await DriveHelpers.CheckEmptyDrive(navigationPath))
					return;

				await NavigationHelpers.OpenPathInNewTab(navigationPath);
			}
		}

		private void Button_RightTapped(object sender, RightTappedRoutedEventArgs e)
		{
			ViewModel.Button_RightTapped(sender, e);
		}

		private async void GoToStorageSense_Click(object sender, RoutedEventArgs e)
		{
			if (sender is Button button && button.Tag is string clickedCard)
			{
				await StorageSenseHelper.OpenStorageSenseAsync(clickedCard);
			}
		}
	}
}
