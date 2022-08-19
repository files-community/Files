using System.Collections.Generic;
using System.ComponentModel;

namespace Files.Backend.Services.Settings
{
    public interface IPreferencesSettingsService : IBaseSettingsService, INotifyPropertyChanged
    {
        /// <summary>
        /// Gets or sets a value indicating whether or not to show the delete confirmation dialog when deleting items.
        /// </summary>
        bool ShowConfirmDeleteDialog { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not to open folders in new tab.
        /// </summary>
        bool OpenFoldersInNewTab { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not file extensions should be visible.
        /// </summary>
        bool ShowFileExtensions { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not media thumbnails should be visible.
        /// </summary>
        bool ShowThumbnails { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not hidden items should be visible.
        /// </summary>
        bool AreHiddenItemsVisible { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not system items should be visible.
        /// </summary>
        bool AreSystemItemsHidden { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not alternate data streams should be visible.
        /// </summary>
        bool AreAlternateStreamsVisible { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not to display dot files.
        /// </summary>
        bool ShowDotFiles{ get; set; }

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
        /// Gets or sets a value indicating whether or not to enable file tags feature.
        /// </summary>
        bool AreFileTagsEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not to show folder size.
        /// </summary>
        bool ShowFolderSize { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not to navigate to a specific location when launching the app.
        /// </summary>
        bool OpenSpecificPageOnStartup { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the default startup location.
        /// </summary>
        string OpenSpecificPageOnStartupPath { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not continue the last session whenever the app is launched.
        /// </summary>
        bool ContinueLastSessionOnStartUp { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not to open a page when the app is launched.
        /// </summary>
        bool OpenNewTabOnStartup { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not opening the app from the jumplist should open the directory in a new instance.
        /// </summary>
        bool AlwaysOpenNewInstance { get; set; }

        /// <summary>
        /// A list containing all paths to open at startup.
        /// </summary>
        List<string> TabsOnStartupList { get; set; }

        /// <summary>
        /// A list containing all paths to tabs closed on last session.
        /// </summary>
        List<string> LastSessionTabList { get; set; }
    }
}
