using System.ComponentModel;

namespace Files.Services
{
    public interface IWidgetsSettingsService : INotifyPropertyChanged
    {
        /// <summary>
        /// Gets or sets a value indicating whether or not the library cards widget should be visible.
        /// </summary>
        bool ShowFoldersWidget { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not the recent files widget should be visible.
        /// </summary>
        bool ShowRecentFilesWidget { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not the drives widget should be visible.
        /// </summary>
        bool ShowDrivesWidget { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not the Bundles widget should be visible.
        /// </summary>
        bool ShowBundlesWidget { get; set; }
    }
}
