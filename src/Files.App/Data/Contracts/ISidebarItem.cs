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

		public SidebarContextFlyoutOptions MenuOptions { get; }
	}

	public enum SidebarItemType
	{
		Drive,
		LinuxDistro,
		Location,
		FileTag
	}

	public enum SidebarSectionType
	{
		Home,
		Favorites,
		Library,
		Drives,
		CloudDrives,
		Network,
		WSL,
		FileTag
	}

	// TODO: Replace with an enum
	public class SidebarContextFlyoutOptions
	{
		public bool IsLibrariesHeader { get; set; }

		public bool ShowHideSection { get; set; }

		public bool IsLocationItem { get; set; }

		public bool ShowUnpinItem { get; set; }

		public bool ShowProperties { get; set; }

		public bool ShowEmptyRecycleBin { get; set; }

		public bool ShowEjectDevice { get; set; }

		public bool ShowFormatDrive { get; set; }

		public bool ShowShellItems { get; set; }
	}
}
