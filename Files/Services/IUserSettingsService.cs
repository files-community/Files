using Files.Models.JsonSettings;

namespace Files.Services
{
    public interface IUserSettingsService
    {
        ISettingsSharingContext GetContext();

        /// <summary>
        /// Gets or sets a value indicating the width of the sidebar pane when open.
        /// </summary>
        double SidebarWidthPx { get; set; }

        /// <summary>
        /// Gets or sets a value indicating if the sidebar pane should be open or closed.
        /// </summary>
        bool IsSidebarOpen { get; set; }

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
