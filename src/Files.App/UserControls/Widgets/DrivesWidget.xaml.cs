// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using Windows.UI.Core;

namespace Files.App.UserControls.Widgets
{
	public sealed partial class DrivesWidget : UserControl
	{
		private DrivesWidgetViewModel ViewModel { get; } = Ioc.Default.GetRequiredService<DrivesWidgetViewModel>();

		public DrivesWidget()
		{
			InitializeComponent();
		}

		private async void Button_Click(object sender, RoutedEventArgs e)
		{
			string ClickedCard = (sender as Button).Tag.ToString();
			string NavigationPath = ClickedCard;

			await ViewModel.Open(NavigationPath);
		}

		private async void Button_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			// Check if middle click
			if (!e.GetCurrentPoint(null).Properties.IsMiddleButtonPressed)
				return;

			string navigationPath = (sender as Button).Tag.ToString();

			if (await DriveHelpers.CheckEmptyDrive(navigationPath))
				return;

			await NavigationHelpers.OpenPathInNewTab(navigationPath);
		}

		private void GoToStorageSense_Click(object sender, RoutedEventArgs e)
		{
			string clickedCard = (sender as Button).Tag.ToString();

			StorageSenseHelper.OpenStorageSenseAsync(clickedCard);
		}
	}
}
