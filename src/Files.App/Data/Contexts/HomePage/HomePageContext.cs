// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;
using System.Collections.Immutable;

namespace Files.App.Data.Contexts
{
	internal class HomePageContext : ObservableObject, IHomePageContext
	{
		private static readonly IImmutableList<WidgetFileTagCardItem> emptyTaggedItems = Enumerable.Empty<WidgetFileTagCardItem>().ToImmutableList();

		public bool IsAnyItemRightClicked => RightClickedItem is not null;

		private WidgetCardItem? rightClickedItem = null;
		public WidgetCardItem? RightClickedItem
		{
			get => rightClickedItem;
			set => SetProperty(ref rightClickedItem, value);
		}

		private CommandBarFlyout? itemContextFlyoutMenu = null;
		public CommandBarFlyout? ItemContextFlyoutMenu => itemContextFlyoutMenu;

		private IReadOnlyList<WidgetFileTagCardItem> selectedTaggedItems = emptyTaggedItems;
		public IReadOnlyList<WidgetFileTagCardItem> SelectedTaggedItems
		{
			get => selectedTaggedItems;
			set => selectedTaggedItems = value ?? emptyTaggedItems;
		}

		public HomePageContext()
		{
			BaseWidgetViewModel.RightClickedItemChanged += HomePageWidget_RightClickedItemChanged;
			FileTagsWidgetViewModel.SelectedTaggedItemsChanged += FileTagsWidget_SelectedTaggedItemsChanged;
		}

		private void FileTagsWidget_SelectedTaggedItemsChanged(object? sender, IEnumerable<WidgetFileTagCardItem> e)
		{
			SetProperty(ref selectedTaggedItems, e.ToList());
		}

		private void HomePageWidget_RightClickedItemChanged(object? sender, WidgetsRightClickedItemChangedEventArgs e)
		{
			RightClickedItem = e.Item;
			OnPropertyChanged(nameof(IsAnyItemRightClicked));

			SetProperty(ref itemContextFlyoutMenu, e.Flyout, nameof(ItemContextFlyoutMenu));
		}
	}
}
