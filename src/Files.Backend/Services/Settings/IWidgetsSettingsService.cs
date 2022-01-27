using System.ComponentModel;

namespace Files.Backend.Services.Settings
{
    public interface IWidgetsSettingsService : IBaseSettingsService, INotifyPropertyChanged
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

        bool FoldersWidgetExpanded { get; set; }

        bool RecentFilesWidgetExpanded { get; set; }

        bool DrivesWidgetExpanded { get; set; }

        bool BundlesWidgetExpanded { get; set; }
    }
}
