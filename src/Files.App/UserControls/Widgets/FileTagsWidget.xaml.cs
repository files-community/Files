// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Files.App.UserControls.Widgets
{
	/// <summary>
	/// Represents group of control displays a list of <see cref="WidgetFileTagsContainerItem"/>
	/// and its inner items with <see cref="WidgetFileTagCardItem"/>.
	/// </summary>
	public sealed partial class FileTagsWidget : UserControl
	{
		public FileTagsWidgetViewModel ViewModel { get; } = new();

		public FileTagsWidget()
		{
			InitializeComponent();
		}

		private void AdaptiveGridView_RightTapped(object sender, RightTappedRoutedEventArgs e)
		{
			ViewModel.ShowContextFlyout(sender, e);
		}

		private void FileTagItem_ItemClick(object sender, ItemClickEventArgs e)
		{
			if (e.ClickedItem is WidgetFileTagCardItem item)
				item.ClickCommand.Execute(null);
		}
	}
}
