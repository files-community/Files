// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.Contexts
{
	public class SideBarContext : ObservableObject, ISideBarContext
	{
		private SidebarViewModel SideBarViewModel { get; } = Ioc.Default.GetRequiredService<SidebarViewModel>();

		public bool IsItemRightClicked
			=> _RightClickedItem is not null;

		private ILocatableSideBarItem? _RightClickedItem;
		public ILocatableSideBarItem? RightClickedItem
		{
			get => _RightClickedItem;
			set
			{
				if (SetProperty(ref _RightClickedItem, value))
					OnPropertyChanged(nameof(IsItemRightClicked));
			}
		}

		public SideBarContext()
		{
			SideBarViewModel.RightClickedItemChanged += SideBar_RightClickedItemChanged;
		}

		private void SideBar_RightClickedItemChanged(object? sender, SideBarRightClickedItemChangedEventArgs e)
		{
			RightClickedItem = e.Item;
		}
	}
}
