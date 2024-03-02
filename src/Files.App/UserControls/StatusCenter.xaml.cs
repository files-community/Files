// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.UserControls
{
	public sealed partial class StatusCenter : UserControl
	{
		public StatusCenterViewModel ViewModel;

		public StatusCenter()
		{
			InitializeComponent();

			ViewModel = Ioc.Default.GetRequiredService<StatusCenterViewModel>();
		}

		// TODO: Convert into a ICommand
		private void CloseAllItemsButton_Click(object sender, RoutedEventArgs e)
		{
			ViewModel.RemoveAllCompletedItems();
		}

		private void CloseItemButton_Click(object sender, RoutedEventArgs e)
		{
			if (sender is Button button && button.DataContext is StatusCenterItem item)
				ViewModel.RemoveItem(item);
		}
	}
}
