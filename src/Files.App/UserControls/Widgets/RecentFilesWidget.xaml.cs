// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Files.App.UserControls.Widgets
{
	/// <summary>
	/// Represents group of control displays a list of recent folders with <see cref="WidgetFolderCardItem"/>.
	/// </summary>
	public sealed partial class RecentFilesWidget : UserControl
	{
		public RecentFilesWidgetViewModel ViewModel { get; set; } = new();

		public RecentFilesWidget()
		{
			InitializeComponent();
		}

		private void RecentFilesView_ItemClick(object sender, ItemClickEventArgs e)
		{
			if (e.ClickedItem is not RecentItem item)
				return;

			ViewModel.NavigateToPath(item.RecentPath);
		}

		private void ListView_RightTapped(object sender, RightTappedRoutedEventArgs e)
		{
			ViewModel.BuildItemContextMenu(e.OriginalSource, e);
		}
	}
}
