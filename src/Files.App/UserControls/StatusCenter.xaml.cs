// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.UserControls
{
	public sealed partial class StatusCenter : UserControl
	{
		public StatusCenterViewModel OngoingTasksViewModel { get; set; } = null!;

		public StatusCenter()
		{
			InitializeComponent();
		}

		// Dismiss banner button event handler
		private void DismissBanner(object sender, RoutedEventArgs e)
		{
			StatusCenterItem itemToDismiss = (sender as Button).DataContext as StatusCenterItem;
			OngoingTasksViewModel.CloseBanner(itemToDismiss);
		}

		// Primary action button click
		private async void Button_Click_1(object sender, RoutedEventArgs e)
		{
			StatusCenterItem itemToDismiss = (sender as Button).DataContext as StatusCenterItem;
			await Task.Run(itemToDismiss.PrimaryButtonClick);
			OngoingTasksViewModel.CloseBanner(itemToDismiss);
		}

		private void DismissAllBannersButton_Click(object sender, RoutedEventArgs e)
		{
			for (int i = OngoingTasksViewModel.StatusBannersSource.Count - 1; i >= 0; i--)
			{
				var itemToDismiss = OngoingTasksViewModel.StatusBannersSource[i];
				if (!itemToDismiss.IsProgressing)
				{
					OngoingTasksViewModel.CloseBanner(itemToDismiss);
				}
			}
		}
	}
}
