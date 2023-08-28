// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Utils.StatusCenter;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.UserControls
{
	public sealed partial class StatusCenter : UserControl
	{
		public StatusCenterViewModel OngoingTasksViewModel;

		public StatusCenter()
		{
			InitializeComponent();
		}

		private void CloseAllItemsButton_Click(object sender, RoutedEventArgs e)
		{
			OngoingTasksViewModel.StatusCenterItems.ForEach((x) =>
			{
				if (x.IsProgressing)
					OngoingTasksViewModel.CloseBanner(x);
			});

			OngoingTasksViewModel.StatusCenterItems.Clear();
		}

		private void CloseItemButton_Click(object sender, RoutedEventArgs e)
		{
			if (sender is Button button && button.DataContext is StatusCenterItem item)
			{
				OngoingTasksViewModel.CloseBanner(item);
			}
		}
	}
}
