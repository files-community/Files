// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;

namespace Files.App.Data.Items
{
	public interface INavigationControlItem : IComparable<INavigationControlItem>, INotifyPropertyChanged
	{
		public string Text { get; }

		public string Path { get; }

		public SectionType Section { get; }

		public string ToolTipText { get; }

		public NavigationControlItemType ItemType { get; }

		public ContextMenuOptions MenuOptions { get; }

		public BulkConcurrentObservableCollection<INavigationControlItem>? ChildItems { get; }

		public IconSource? GenerateIconSource();
	}

	public enum NavigationControlItemType
	{
		Drive,
		LinuxDistro,
		Location,
		CloudDrive,
		FileTag
	}

	public enum SectionType
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

	public class ContextMenuOptions
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
