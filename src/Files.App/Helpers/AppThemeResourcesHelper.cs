using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Files.App.Helpers
{
	public sealed class AppThemeResourcesHelper
	{
		/// <summary>
		/// Forces the application to use the correct resource styles
		/// </summary>
		public void ApplyResources()
		{
			// Get the index of the current theme
			var selTheme = ThemeHelper.RootTheme;

			// Toggle between the themes to force the controls to use the new resource styles
			ThemeHelper.RootTheme = ElementTheme.Dark;
			ThemeHelper.RootTheme = ElementTheme.Light;

			// Restore the theme to the correct theme
			ThemeHelper.RootTheme = selTheme;
		}

		/// <summary>
		/// Overrides the xaml resource for App.Theme.BackgroundBrush
		/// </summary>
		/// <param name="appThemeBackgroundColor"></param>
		public void SetAppThemeBackgroundColor(Color appThemeBackgroundColor)
		{
			Application.Current.Resources["App.Theme.BackgroundBrush"] = appThemeBackgroundColor;
		}

		/// <summary>
		/// Overrides the xaml resource for App.Theme.Sidebar.BackgroundBrush
		/// </summary>
		/// <param name="appThemeSidebarBackgroundColor"></param>
		public void SetAppThemeSidebarBackgroundColor(Color appThemeSidebarBackgroundColor)
		{
			Application.Current.Resources["App.Theme.Sidebar.BackgroundBrush"] = appThemeSidebarBackgroundColor;
		}

		/// <summary>
		/// Overrides the xaml resource for App.Theme.FileArea.BackgroundBrush
		/// </summary>
		/// <param name="appThemeFileAreaBackgroundColor"></param>
		public void SetAppThemeFileAreaBackgroundColor(Color appThemeFileAreaBackgroundColor)
		{
			Application.Current.Resources["App.Theme.FileArea.BackgroundBrush"] = appThemeFileAreaBackgroundColor;
		}

		/// <summary>
		/// Overrides the xaml resource for the list view item height
		/// </summary>
		/// <param name="useCompactSpacing"></param>
		public void SetCompactSpacing(bool useCompactSpacing)
		{
			if (useCompactSpacing)
			{
				Application.Current.Resources["ListItemHeight"] = 24;
				Application.Current.Resources["NavigationViewItemOnLeftMinHeight"] = 20;
			}
			else
			{
				Application.Current.Resources["ListItemHeight"] = 36;
				Application.Current.Resources["NavigationViewItemOnLeftMinHeight"] = 32;
			}
		}
	}
}
