// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;

namespace Files.App.Data.Contracts
{
	public interface INavigationControlItem : IComparable<INavigationControlItem>, INotifyPropertyChanged
	{
		/// <summary>
		/// Gets the children of this item that will be rendered as child elements of the SidebarItem
		/// </summary>
		object? Children { get; }

		/// <summary>
		/// Gets the icon source used to generate the icon for the SidebarItem
		/// </summary>
		FrameworkElement? IconSource { get; }

		/// <summary>
		/// Gets item decorator for the given item.
		/// </summary>
		FrameworkElement? ItemDecorator { get => null; }

		/// <summary>
		/// Gets or sets a value that indicates whether the SidebarItem is expanded and the children are visible 
		/// or if it is collapsed and children are not visible.
		/// </summary>
		bool IsExpanded { get; set; }

		/// <summary>
		/// Gets the text of this item that will be rendered as the label of the SidebarItem
		/// </summary>
		string Text { get; }

		/// <summary>
		/// Gets the tooltip used when hovering over this item in the sidebar
		/// </summary>
		object ToolTip { get; }

		public string Path { get; }

		public SectionType Section { get; }

		public NavigationControlItemType ItemType { get; }

		public ContextMenuOptions MenuOptions { get; }
	}

	public enum NavigationControlItemType
	{
		Drive,
		LinuxDistro,
		Location,
		FileTag
	}

	public enum SectionType
	{
		Home,
		Pinned,
		Library,
		Drives,
		CloudDrives,
		Network,
		WSL,
		FileTag,

		Footer,
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
