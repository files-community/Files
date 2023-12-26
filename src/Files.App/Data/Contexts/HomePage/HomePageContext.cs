// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.UserControls.Widgets;
using Files.App.ViewModels.Widgets;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Immutable;

namespace Files.App.Data.Contexts
{
	internal class HomePageContext : ObservableObject, IHomePageContext
	{
		private static readonly IImmutableList<FileTagsItemViewModel> emptyTaggedItems = Enumerable.Empty<FileTagsItemViewModel>().ToImmutableList();

		public bool IsAnyItemRightClicked => rightClickedItem is not null;

		private WidgetCardItem? rightClickedItem = null;
		public WidgetCardItem? RightClickedItem => rightClickedItem;

		private CommandBarFlyout? itemContextFlyoutMenu = null;
		public CommandBarFlyout? ItemContextFlyoutMenu => itemContextFlyoutMenu;

		private IReadOnlyList<FileTagsItemViewModel> selectedTaggedItems = emptyTaggedItems;
		public IReadOnlyList<FileTagsItemViewModel> SelectedTaggedItems
		{
			get => selectedTaggedItems;
			set => selectedTaggedItems = value ?? emptyTaggedItems;
		}

		public HomePageContext()
		{
			HomePageWidget.RightClickedItemChanged += HomePageWidget_RightClickedItemChanged;
			FileTagsWidget.SelectedTaggedItemsChanged += FileTagsWidget_SelectedTaggedItemsChanged;
		}

		private void FileTagsWidget_SelectedTaggedItemsChanged(object? sender, IEnumerable<FileTagsItemViewModel> e)
		{
			SetProperty(ref selectedTaggedItems, e.ToList());
		}

		private void HomePageWidget_RightClickedItemChanged(object? sender, WidgetsRightClickedItemChangedEventArgs e)
		{
			if (SetProperty(ref rightClickedItem, e.Item, nameof(RightClickedItem)))
				OnPropertyChanged(nameof(IsAnyItemRightClicked));

			SetProperty(ref itemContextFlyoutMenu, e.Flyout, nameof(ItemContextFlyoutMenu));
		}
	}
}
