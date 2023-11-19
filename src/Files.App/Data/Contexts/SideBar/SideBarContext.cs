// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.Contexts
{
	internal class SideBarContext : ObservableObject, ISideBarContext
	{
		public static event EventHandler<SideBarRightClickedItemChangedEventArgs>? RightClickedItemChanged;

		public bool IsAnyItemRightClicked
			=> _RightClickedItem is not null;

		private ILocatableSideBarItem? _RightClickedItem;
		public ILocatableSideBarItem? RightClickedItem
		{
			get => _RightClickedItem;
			set
			{
				if (SetProperty(ref _RightClickedItem, value))
					OnPropertyChanged(nameof(IsAnyItemRightClicked));
			}
		}

		public SideBarContext()
		{
			RightClickedItemChanged += SideBar_RightClickedItemChanged;
		}

		private void SideBar_RightClickedItemChanged(object? sender, SideBarRightClickedItemChangedEventArgs e)
		{
			RightClickedItem = e.Item;
		}
	}
}
