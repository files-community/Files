// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Files.App.UserControls.Widgets
{
	public sealed partial class QuickAccessWidget : UserControl
	{
		// Properties

		public QuickAccessWidgetViewModel? ViewModel { get; set; }

		// Constructor

		public QuickAccessWidget()
		{
			InitializeComponent();
		}

		// Event methods

		private void CardItemButton_RightTapped(object sender, RightTappedRoutedEventArgs e)
		{
			ViewModel!.BuildRightClickContextMenu(sender, e);
			e.Handled = true;
		}

		private async void CardItemButton_Click(object sender, RoutedEventArgs e)
		{
			await ViewModel!.GoToItem(sender);
		}

		private async void CardItemButton_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			// Check if the click mode is middle click
			if (e.GetCurrentPoint(null).Properties.IsMiddleButtonPressed)
			{
				string navigationPath = ((Button)sender).Tag.ToString()!;
				await NavigationHelpers.OpenPathInNewTab(navigationPath);
			}
		}
	}
}
