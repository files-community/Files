// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinRT;

namespace Files.App.UserControls.StatusCenter
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
			ViewModel.RemoveAllCompletedItems();
		}

		[DynamicWindowsRuntimeCast(typeof(Button))]
		private void CloseItemButton_Click(object sender, RoutedEventArgs e)
		{
			if (sender is Button button && button.DataContext is StatusCenterItem item)
				ViewModel.RemoveItem(item);
		}

		[DynamicWindowsRuntimeCast(typeof(Button))]
		private void ExpandCollapseChevronItemButton_Click(object sender, RoutedEventArgs e)
		{
			if (sender is Button button && button.DataContext is StatusCenterItem item)
			{
				var buttonAnimatedIcon = button.FindDescendant<AnimatedIcon>();

				if (buttonAnimatedIcon is not null)
					AnimatedIcon.SetState(buttonAnimatedIcon, item.IsExpanded ? "NormalOff" : "NormalOn");

				item.IsExpanded = !item.IsExpanded;
			}
		}
	}
}
