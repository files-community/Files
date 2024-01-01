// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Files.App.UserControls.Widgets
{
	public sealed partial class RecentFilesWidget : UserControl
	{
		// Properties

		public RecentFilesWidgetViewModel? ViewModel { get; set; }

		// Constructor

		public RecentFilesWidget()
		{
			InitializeComponent();
		}

		// Event methods

		private void RecentFilesListView_RightTapped(object sender, RightTappedRoutedEventArgs e)
		{
			ViewModel!.ShowRightClickContextMenu(sender, e);
		}

		private void RecentFilesListView_ItemClick(object sender, ItemClickEventArgs e)
		{
			ViewModel!.GoToItem((RecentItem)e.ClickedItem);
		}
	}
}
