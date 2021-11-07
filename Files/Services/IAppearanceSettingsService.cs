using System.ComponentModel;

namespace Files.Services
{
    public interface IAppearanceSettingsService : INotifyPropertyChanged
    {
        /// <summary>
        /// Gets or sets a value indicating whether or not to move overflow menu items into a sub menu.
        /// </summary>
        bool MoveOverflowMenuItemsToSubMenu { get; set; }

        #region Internal Settings

        /// <summary>
        /// Gets or sets a value indicating the width of the sidebar pane when open.
        /// </summary>
        double SidebarWidth { get; set; }

        /// <summary>
        /// Gets or sets a value indicating if the sidebar pane should be open or closed.
        /// </summary>
        bool IsSidebarOpen { get; set; }

        #endregion

        /// <summary>
        /// Gets or sets a value indicating whether or not to show the Favorites section on the sidebar.
        /// </summary>
        bool ShowFavoritesSection { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not to show the library section on the sidebar.
        /// </summary>
        bool ShowLibrarySection { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [show drives section].
        /// </summary>
        bool ShowDrivesSection { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [show cloud drives section].
        /// </summary>
        bool ShowCloudDrivesSection { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [show network drives section].
        /// </summary>
        bool ShowNetworkDrivesSection { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [show wsl section].
        /// </summary>
        bool ShowWslSection { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not recycle bin should be pinned to the sidebar.
        /// </summary>
        bool PinRecycleBinToSidebar { get; set; }
    }
}
