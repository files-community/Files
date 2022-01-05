using System.ComponentModel;

namespace Files.Services
{
    public interface IMultitaskingSettingsService : INotifyPropertyChanged
    {
        /// <summary>
        /// Gets or sets a value indicating whether or not to enable the vertical tab flyout.
        /// </summary>
        bool IsVerticalTabFlyoutEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not to enable dual pane feature.
        /// </summary>
        bool IsDualPaneEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not to always open a second pane when opening a new tab.
        /// </summary>
        bool AlwaysOpenDualPaneInNewTab { get; set; }
    }
}
