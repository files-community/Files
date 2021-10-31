using System.ComponentModel;

namespace Files.Services
{
    public interface IFilesAndFoldersSettingsService : INotifyPropertyChanged
    {
        /// <summary>
        /// Gets or sets a value indicating whether or not file extensions should be visible.
        /// </summary>
        bool ShowFileExtensions { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not hidden items should be visible.
        /// </summary>
        bool AreHiddenItemsVisible { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not system items should be visible.
        /// </summary>
        bool AreSystemItemsHidden { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not files should be sorted together with folders.
        /// </summary>
        bool ListAndSortDirectoriesAlongsideFiles { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not files should open with one click.
        /// </summary>
        bool OpenFilesWithOneClick { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not folders should open with one click.
        /// </summary>
        bool OpenFoldersWithOneClick { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not to search unindexed items.
        /// </summary>
        bool SearchUnindexedItems { get; set; }

        /// <summary>
        /// Enables saving a unique layout mode, gridview size and sort direction per folder
        /// </summary>
        bool AreLayoutPreferencesPerFolder { get; set; }

        /// <summary>
        /// Enables adaptive layout that adjusts layout mode based on the context of the directory
        /// </summary>
        bool AdaptiveLayoutEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not to enable file tags feature.
        /// </summary>
        bool AreFileTagsEnabled { get; set; }
    }
}
