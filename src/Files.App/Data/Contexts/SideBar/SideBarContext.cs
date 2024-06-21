// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;

namespace Files.App.Data.Contexts
{
	/// <inheritdoc cref="ISidebarContext"/>
	internal sealed class SidebarContext : ObservableObject, ISidebarContext
	{
		private readonly PinnedFoldersManager favoriteModel = App.QuickAccessManager.Model;

		private int PinnedFolderItemIndex =>
			IsItemRightClicked
				? favoriteModel.IndexOfItem(_RightClickedItem!)
				: -1;

		private INavigationControlItem? _RightClickedItem = null;
		public INavigationControlItem? RightClickedItem => _RightClickedItem;

		private CommandBarFlyout? itemContextFlyoutMenu = null;
		public CommandBarFlyout? ItemContextFlyoutMenu => itemContextFlyoutMenu;

		public bool IsItemRightClicked =>
			_RightClickedItem is not null;

		public bool IsPinnedFolderItem =>
			IsItemRightClicked &&
			_RightClickedItem!.Section is SectionType.Pinned &&
			PinnedFolderItemIndex is not -1;

		public DriveItem? OpenDriveItem
			=> _RightClickedItem as DriveItem;

		public SidebarContext()
		{
			SidebarViewModel.RightClickedItemChanged += SidebarControl_RightClickedItemChanged;
		}

		public void SidebarControl_RightClickedItemChanged(object? sender, SidebarRightClickedItemChangedEventArgs e)
		{
			if (SetProperty(ref _RightClickedItem, e.Item, nameof(RightClickedItem)))
			{
				OnPropertyChanged(nameof(IsItemRightClicked));
				OnPropertyChanged(nameof(PinnedFolderItemIndex));
				OnPropertyChanged(nameof(IsPinnedFolderItem));
				OnPropertyChanged(nameof(OpenDriveItem));

				SetProperty(ref itemContextFlyoutMenu, e.Flyout, nameof(ItemContextFlyoutMenu));
			}
		}
	}
}
