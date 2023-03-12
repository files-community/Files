using CommunityToolkit.Mvvm.ComponentModel;
using Files.App.DataModels;
using Files.App.DataModels.NavigationControlItems;
using Files.App.Filesystem;
using Files.App.UserControls;

namespace Files.App.Contexts
{
	internal class SidebarContext : ObservableObject, ISidebarContext
	{
		private INavigationControlItem? rightClickedItem = null;
		public INavigationControlItem? RightClickedItem => rightClickedItem;

		public bool IsAnyItemRightClicked => rightClickedItem is not null;

		private readonly SidebarPinnedModel favoriteModel = App.QuickAccessManager.Model;

		private int FavoriteIndex => IsAnyItemRightClicked
			? favoriteModel.IndexOfItem(rightClickedItem!)
			: -1;

		public bool IsFavoriteItem => 
			IsAnyItemRightClicked && 
			rightClickedItem!.Section is SectionType.Favorites &&
			FavoriteIndex is not -1;

		public DriveItem? OpenDriveItem => rightClickedItem as DriveItem;

		public SidebarContext()
		{
			SidebarControl.RightClickedItemChanged += SidebarControl_RightClickedItemChanged;
		}

		private void SidebarControl_RightClickedItemChanged(object? sender, INavigationControlItem? e)
		{
			if (SetProperty(ref rightClickedItem, e, nameof(RightClickedItem)))
			{
				OnPropertyChanged(nameof(IsAnyItemRightClicked));
				OnPropertyChanged(nameof(FavoriteIndex));
				OnPropertyChanged(nameof(IsFavoriteItem));
				OnPropertyChanged(nameof(OpenDriveItem));
			}
		}
	}
}
