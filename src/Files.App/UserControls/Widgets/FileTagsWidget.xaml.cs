// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Files.App.UserControls.Widgets
{
	public sealed partial class FileTagsWidget : UserControl
	{
		// Properties

		public FileTagsWidgetViewModel? ViewModel { get; set; }

		// Constructor

		public FileTagsWidget()
		{
			InitializeComponent();
		}

		// Event methods

		private void FileTagsContainerAdaptiveGridView_RightTapped(object sender, RightTappedRoutedEventArgs e)
		{
			ViewModel!.ShowRightClickContextMenu(sender, e);
			e.Handled = true;
		}

		private async void FileTagsContainerAdaptiveGridView_ItemClick(object sender, ItemClickEventArgs e)
		{
			if (e.ClickedItem is WidgetFileTagsItem item)
				await item.ClickCommand.ExecuteAsync(null);
		}
	}
}
