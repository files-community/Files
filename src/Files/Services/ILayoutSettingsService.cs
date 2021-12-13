using Files.Enums;
using System.ComponentModel;

namespace Files.Services
{
    public interface ILayoutSettingsService : INotifyPropertyChanged
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

        int DefaultGridViewSize { get; set; }

        FolderLayoutModes DefaultLayoutMode { get; set; }

        SortDirection DefaultDirectorySortDirection { get; set; }

        SortOption DefaultDirectorySortOption { get; set; }

        GroupOption DefaultDirectoryGroupOption { get; set; }
    }
}
