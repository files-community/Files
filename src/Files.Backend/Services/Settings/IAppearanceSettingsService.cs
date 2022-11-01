using System.ComponentModel;

namespace Files.Backend.Services.Settings
{
	public interface IAppearanceSettingsService : IBaseSettingsService, INotifyPropertyChanged
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
		/// Gets or sets a value indicating whether [show wsl section].
		/// </summary>
		bool ShowFileTagsSection { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not to use the compact styles.
		/// </summary>
		bool UseCompactStyles { get; set; }

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

		/// <summary>
		/// Gets or sets a value indicating wheter or not the folders widget section is expanded.
		/// </summary>
		bool FoldersWidgetExpanded { get; set; }

		/// <summary>
		/// Gets or sets a value indicating wheter or not the recent files widget section is expanded.
		/// </summary>
		bool RecentFilesWidgetExpanded { get; set; }

		/// <summary>
		/// Gets or sets a value indicating wheter or not the drives widget section is expanded.
		/// </summary>
		bool DrivesWidgetExpanded { get; set; }

		/// <summary>
		/// Gets or sets a value indicating wheter or not the Bundles widget section is expanded.
		/// </summary>
		bool BundlesWidgetExpanded { get; set; }
	}
}
