// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Contracts
{
	public interface ILayoutSettingsService : IBaseSettingsService, INotifyPropertyChanged
	{
		/// <summary>
		/// Sync folder preferences across all directories
		/// </summary>
		bool SyncFolderPreferencesAcrossDirectories { get; set; }

		/// <summary>
		/// Gets or sets a value indicating the default layout mode.
		/// </summary>
		FolderLayoutModes DefaultLayoutMode { get; set; }

		/// <summary>
		/// Gets or sets a value indicating the default sorting option.
		/// </summary>
		SortOption DefaultSortOption { get; set; }

		/// <summary>
		/// Gets or sets the column used for sorting when the default sorting option is a column.
		/// </summary>
		string DefaultSortColumnId { get; set; }

		/// <summary>
		/// Gets or sets a value indicating the default sorting direction.
		/// </summary>
		SortDirection DefaultDirectorySortDirection { get; set; }

		/// <summary>
		/// Gets or sets a value indicating if directories should be sorted alongside files by.
		/// </summary>
		bool DefaultSortDirectoriesAlongsideFiles { get; set; }

		/// <summary>
		/// Gets or sets a value indicating if files should be sorted first.
		/// </summary>
		bool DefaultSortFilesFirst { get; set; }

		/// <summary>
		/// Gets or sets a value indicating the default grouping option.
		/// </summary>
		GroupOption DefaultGroupOption { get; set; }

		/// <summary>
		/// Gets or sets the column used for grouping when the default grouping option is a column.
		/// </summary>
		string DefaultGroupColumnId { get; set; }

		/// <summary>
		/// Gets or sets the visible default columns of the details layout in display order.
		/// </summary>
		List<string>? DetailsLayoutColumns { get; set; }

		/// <summary>
		/// Gets or sets the default column widths of the details layout as "id|width" entries.
		/// </summary>
		List<string>? DetailsLayoutColumnWidths { get; set; }

		/// <summary>
		/// Gets or sets a value indicating the default grouping direction.
		/// </summary>
		SortDirection DefaultDirectoryGroupDirection { get; set; }

		/// <summary>
		/// Gets or sets a value indicating the unit for grouping by date.
		/// </summary>
		GroupByDateUnit DefaultGroupByDateUnit { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not columns in the Details layout should
		/// automatically resize to fit content on navigation, item add/remove, and rename.
		/// </summary>
		bool AutoSizeColumnsInDetailsLayout { get; set; }

		/// <summary>
		/// Item size in the Details View
		/// </summary>
		DetailsViewSizeKind DetailsViewSize { get; set; }

		/// <summary>
		/// Item size in the List View
		/// </summary>
		ListViewSizeKind ListViewSize { get; set; }

		/// <summary>
		/// Item size in the Cards View
		/// </summary>
		CardsViewSizeKind CardsViewSize { get; set; }

		/// <summary>
		/// Item size in the Grid View
		/// </summary>
		GridViewSizeKind GridViewSize { get; set; }

		/// <summary>
		/// Item size in the Columns View
		/// </summary>
		ColumnsViewSizeKind ColumnsViewSize { get; set; }
	}
}
