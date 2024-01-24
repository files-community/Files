// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.UserControls.Sidebar;

namespace Files.App.Data.Contracts
{
	public interface ISidebarItem : IComparable<ISidebarItem>, INotifyPropertyChanged, ISidebarItemModel
	{
		public new string Text { get; }

		public string Path { get; }

		public SidebarSectionType Section { get; }

		public SidebarItemType ItemType { get; }

		public SidebarContextMenuOptions MenuOptions { get; }
	}
}
