// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.Contexts
{
	/// <inheritdoc cref="ISidebarContext"/>
	internal sealed class SidebarContext : ObservableObject, ISidebarContext
	{
		private readonly IQuickAccessService WindowsQuickAccessService = Ioc.Default.GetRequiredService<IQuickAccessService>();

		private INavigationControlItem? _RightClickedItem = null;
		public INavigationControlItem? RightClickedItem => _RightClickedItem;

		public bool IsItemRightClicked =>
			_RightClickedItem is not null;

		public bool IsPinnedFolderItem =>
			IsItemRightClicked &&
			_RightClickedItem!.Section is SectionType.Pinned &&
			WindowsQuickAccessService.IsPinned(_RightClickedItem.Path);

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
				OnPropertyChanged(nameof(IsPinnedFolderItem));
				OnPropertyChanged(nameof(OpenDriveItem));
			}
		}
	}
}
