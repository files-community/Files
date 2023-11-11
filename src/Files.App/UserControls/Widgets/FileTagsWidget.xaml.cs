// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.ViewModels.Widgets;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Files.App.UserControls.Widgets
{
	public sealed partial class FileTagsWidget : UserControl
	{
		private FileTagsWidgetViewModel ViewModel { get; } = Ioc.Default.GetRequiredService<FileTagsWidgetViewModel>();
		private IQuickAccessService QuickAccessService { get; } = Ioc.Default.GetRequiredService<IQuickAccessService>();

		public FileTagsWidget()
		{
			InitializeComponent();
		}

		private async void FileTagItem_ItemClick(object sender, ItemClickEventArgs e)
		{
			if (e.ClickedItem is FileTagsItemViewModel itemViewModel)
				await itemViewModel.ClickCommand.ExecuteAsync(null);
		}

		private void AdaptiveGridView_RightTapped(object sender, RightTappedRoutedEventArgs e)
		{
			if (e.OriginalSource is not FrameworkElement element ||
				element.DataContext is not FileTagsItemViewModel item)
			{
				return;
			}

			ViewModel.LoadContextMenu(
				element,
				e,
				ViewModel.GetItemMenuItems(item, QuickAccessService.IsItemPinned(item.Path), item.IsFolder),
				rightClickedItem: item);
		}
	}
}
