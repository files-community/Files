// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.Contexts
{
	/// <inheritdoc cref="ISidebarContext"/>
	internal class SidebarContext : ObservableObject, ISidebarContext
	{
		private readonly SidebarPinnedModel favoriteModel = App.QuickAccessManager.Model;

		private int FavoriteIndex =>
			IsItemRightClicked
				? favoriteModel.IndexOfItem(_RightClickedItem!)
				: -1;

		private INavigationControlItem? _RightClickedItem = null;
		public INavigationControlItem? RightClickedItem => _RightClickedItem;

		public bool IsItemRightClicked =>
			_RightClickedItem is not null;

		public bool IsFavoriteItem =>
			IsItemRightClicked &&
			_RightClickedItem!.Section is SectionType.Favorites &&
			FavoriteIndex is not -1;

		public DriveItem? OpenDriveItem
			=> _RightClickedItem as DriveItem;

		public SidebarContext()
		{
			SidebarViewModel.RightClickedItemChanged += SidebarControl_RightClickedItemChanged;
		}

		public void SidebarControl_RightClickedItemChanged(object? sender, INavigationControlItem? e)
		{
			if (SetProperty(ref _RightClickedItem, e, nameof(RightClickedItem)))
			{
				OnPropertyChanged(nameof(IsItemRightClicked));
				OnPropertyChanged(nameof(FavoriteIndex));
				OnPropertyChanged(nameof(IsFavoriteItem));
				OnPropertyChanged(nameof(OpenDriveItem));
			}
		}
	}
}
