using CommunityToolkit.Mvvm.ComponentModel;
using Files.App.UserControls.Widgets;
using Files.App.ViewModels.Widgets;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Files.App.Contexts
{
	internal class WidgetsPageContext : ObservableObject, IWidgetsPageContext
	{
		private static readonly IReadOnlyList<FileTagsItemViewModel> emptyTaggedItemsList = Enumerable.Empty<FileTagsItemViewModel>().ToImmutableList();

		private WidgetCardItem? _rightClickedItem = null;
		public WidgetCardItem? RightClickedItem => _rightClickedItem;

		private CommandBarFlyout? _itemContextFlyoutMenu = null;
		public CommandBarFlyout? ItemContextFlyoutMenu => _itemContextFlyoutMenu;

		private IEnumerable<FileTagsItemViewModel> _selectedTaggedItems = emptyTaggedItemsList;
		public IEnumerable<FileTagsItemViewModel> SelectedTaggedItems
		{
			get => _selectedTaggedItems;
			set
			{
				if (value is not null)
					_selectedTaggedItems = value;
				else
					_selectedTaggedItems = emptyTaggedItemsList;
			}
		}

		public bool IsAnyItemRightClicked => _rightClickedItem is not null;

		public WidgetsPageContext()
		{
			HomePageWidget.RightClickedItemChanged += HomePageWidget_RightClickedItemChanged;
		}

		private void HomePageWidget_RightClickedItemChanged(object? sender, (WidgetCardItem, CommandBarFlyout) e)
		{
			var (item, flyout) = e;
			
			if (item != _rightClickedItem)
			{
				_rightClickedItem = item;
				OnPropertyChanged(nameof(RightClickedItem));
				OnPropertyChanged(nameof(IsAnyItemRightClicked));
			}

			if (flyout != _itemContextFlyoutMenu)
			{
				_itemContextFlyoutMenu = flyout;
				OnPropertyChanged(nameof(ItemContextFlyoutMenu));
			}
		}
	}
}
