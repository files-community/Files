using System;
using System.ComponentModel;

namespace Files.Core.Services.Settings
{
	public interface IAppearanceSettingsService : IBaseSettingsService, INotifyPropertyChanged
	{
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
		/// Gets or sets a value indicating whether or not to use the compact styles.
		/// </summary>
		bool UseCompactStyles { get; set; }

		/// <summary>
		/// Gets or sets a value for the app theme background color.
		/// </summary>
		String AppThemeBackgroundColor { get; set; }

		/// <summary>
		/// Gets or sets a value for the app theme address bar background color.
		/// </summary>
		String AppThemeAddressBarBackgroundColor { get; set; }

		/// <summary>
		/// Gets or sets a value for the app theme sidebar background color.
		/// </summary>
		String AppThemeSidebarBackgroundColor { get; set; }

		/// <summary>
		/// Gets or sets a value for the app theme file area background color.
		/// </summary>
		String AppThemeFileAreaBackgroundColor { get; set; }

		/// <summary>
		/// Gets or sets a value for the app theme font family.
		/// </summary>
		String AppThemeFontFamily { get; set; }
	}
}
