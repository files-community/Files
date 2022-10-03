using Files.Shared.Enums;
using System.ComponentModel;

namespace Files.Backend.Services.Settings
{
    public interface ILayoutSettingsService : IBaseSettingsService, INotifyPropertyChanged
    {
        /// <summary>
        /// Gets or sets a value indicating whether or not the date column should be visible.
        /// </summary>
        bool ShowDateColumn { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not the date created column should be visible.
        /// </summary>
        bool ShowDateCreatedColumn { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not the type column should be visible.
        /// </summary>
        bool ShowTypeColumn { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not the size column should be visible.
        /// </summary>
        bool ShowSizeColumn { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not the filetag column should be visible.
        /// </summary>
        bool ShowFileTagColumn { get; set; }

		/// <summary>
		/// Gets or sets a value indicating tags column's default width
		/// </summary>
		double TagColumnWidth { get; set; }

		/// <summary>
		/// Gets or sets a value indicating name column's default width
		/// </summary>
		double NameColumnWidth { get; set; }

		/// <summary>
		/// Gets or sets a value indicating date modified column's default width
		/// </summary>
		double DateModifiedColumnWidth { get; set; }

		/// <summary>
		/// Gets or sets a value indicating item type column's default width
		/// </summary>
		double ItemTypeColumnWidth { get; set; }

		/// <summary>
		/// Gets or sets a value indicating date created column's default width
		/// </summary>
		double DateCreatedColumnWidth { get; set; }

		/// <summary>
		/// Gets or sets a value indicating size column's default width
		/// </summary>
		double SizeColumnWidth { get; set; }

		int DefaultGridViewSize { get; set; }

        FolderLayoutModes DefaultLayoutMode { get; set; }

        SortDirection DefaultDirectorySortDirection { get; set; }

        SortOption DefaultDirectorySortOption { get; set; }

        bool DefaultSortDirectoriesAlongsideFiles { get; set; }

        GroupOption DefaultDirectoryGroupOption { get; set; }
    }
}
