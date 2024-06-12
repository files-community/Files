// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.UserControls.Sidebar;

namespace Files.App.Data.Contracts
{
	public interface ISidebarItem : IComparable<ISidebarItem>, INotifyPropertyChanged, ISidebarItemModel
	{
		public new string Text { get; }

		public string Path { get; }

		public SidebarSectionKind Section { get; }

		public SidebarItemKind ItemType { get; }

		public ContextMenuOptions MenuOptions { get; }
	}

	public enum SidebarItemKind
	{
		Drive,
		LinuxDistro,
		Location,
		FileTag
	}

	public enum SidebarSectionKind
	{
		Home,
		Pinned,
		Library,
		Drives,
		CloudDrives,
		Network,
		WSL,
		FileTag
	}

	public sealed class ContextMenuOptions
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
