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

		private void CloseAllItemsButton_Click(object sender, RoutedEventArgs e)
		{
			ViewModel.StatusCenterItems.ForEach((x) =>
			{
				if (x.IsProgressing)
					ViewModel.CloseBanner(x);
			});

			ViewModel.StatusCenterItems.Clear();
		}

		private void CloseItemButton_Click(object sender, RoutedEventArgs e)
		{
			if (sender is Button button && button.DataContext is StatusCenterItem item)
			{
				ViewModel.CloseBanner(item);
			}
		}
	}
}
