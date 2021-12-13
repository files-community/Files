using System.ComponentModel;

namespace Files.Services
{
    public interface IPreviewPaneSettingsService : INotifyPropertyChanged
    {
        /// <summary>
        /// Gets or sets a value indicating the default volume on media preview.
        /// </summary>
        double PreviewPaneMediaVolume { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the height of the preview pane in a horizontal layout.
        /// </summary>
        double PreviewPaneSizeHorizontalPx { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the width of the preview pane in a vertical layout.
        /// </summary>
        double PreviewPaneSizeVerticalPx { get; set; }

        /// <summary>
        /// Gets or sets a value indicating if the preview pane should be open or closed.
        /// </summary>
        bool PreviewPaneEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating if the preview pane should only show the item preview without the details section
        /// </summary>
        bool ShowPreviewOnly { get; set; }
    }
}
