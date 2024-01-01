// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Files.App.UserControls.Widgets
{
	public sealed partial class DrivesWidget : UserControl
	{
		// Properties

		public DrivesWidgetViewModel? ViewModel { get; set; }

		// Constructor

		public DrivesWidget()
		{
			InitializeComponent();
		}

		// Event methods

		private void CardItemButton_RightTapped(object sender, RightTappedRoutedEventArgs e)
		{
			ViewModel!.ShowRightClickContextMenu(sender, e);
			e.Handled = true;
		}

		private async void CardItemButton_Click(object sender, RoutedEventArgs e)
		{
			await ViewModel!.GoToItem(sender);
		}

		private async void CardItemButton_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			if (!e.GetCurrentPoint(null).Properties.IsMiddleButtonPressed ||
				sender is not Button button)
				return;

			string navigationPath = button.Tag.ToString()!;

			if (await DriveHelpers.CheckEmptyDrive(navigationPath))
				return;

			await NavigationHelpers.OpenPathInNewTab(navigationPath);
		}

		private void GoToStorageSense_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not Button button)
				return;

			string path = button.Tag.ToString() ?? string.Empty;

			_ = StorageSenseHelper.OpenStorageSenseAsync(path);
		}
	}
}
