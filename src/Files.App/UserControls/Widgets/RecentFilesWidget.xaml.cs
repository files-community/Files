// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.ViewModels.UserControls.Widgets;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Files.App.UserControls.Widgets
{
	/// <summary>
	/// Represents group of control displays a list of recent folders with <see cref="WidgetFolderCardItem"/>.
	/// </summary>
	public sealed partial class RecentFilesWidget : UserControl
	{
		private RecentFilesWidgetViewModel ViewModel = new();

		public RecentFilesWidget()
		{
			InitializeComponent();
		}

		private void ListView_RightTapped(object sender, RightTappedRoutedEventArgs e)
		{
			ViewModel.BuildContextFlyout(sender, e);
		}

		private void RecentItemsView_ItemClick(object sender, ItemClickEventArgs e)
		{
			var recentItem = e.ClickedItem as RecentItem;
			RecentFileInvoked?.Invoke(this, new PathNavigationEventArgs()
			{
				ItemPath = recentItem.RecentPath,
				IsFile = recentItem.IsFile
			});
		}
	}
}
