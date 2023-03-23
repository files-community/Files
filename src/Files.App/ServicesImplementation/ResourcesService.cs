using Files.App.Helpers;
using Files.Shared.Services;
using Microsoft.UI.Xaml;
using System.Drawing;

namespace Files.App.ServicesImplementation;

public class ResourcesService : IResourcesService
{
	public void SetAppThemeBackgroundColor(Color appThemeBackgroundColor)
	{
		Application.Current.Resources["App.Theme.BackgroundBrush"] = appThemeBackgroundColor.ToWindowsColor();
	}

	public void SetAppThemeAddressBarBackgroundColor(Color appThemeAddressBarBackgroundColor)
	{
		Application.Current.Resources["App.Theme.AddressBar.BackgroundBrush"] = appThemeAddressBarBackgroundColor.ToWindowsColor();

		// Overrides the selected tab background to match the address bar
		Application.Current.Resources["TabViewItemHeaderBackgroundSelected"] = appThemeAddressBarBackgroundColor.ToWindowsColor();
	}

	
	public void SetAppThemeSidebarBackgroundColor(Color appThemeSidebarBackgroundColor)
	{
		Application.Current.Resources["App.Theme.Sidebar.BackgroundBrush"] = appThemeSidebarBackgroundColor.ToWindowsColor();
	}

	public void SetAppThemeFileAreaBackgroundColor(Color appThemeFileAreaBackgroundColor)
	{
		Application.Current.Resources["App.Theme.FileArea.BackgroundBrush"] = appThemeFileAreaBackgroundColor.ToWindowsColor();
	}

	public void SetAppThemeFontFamily(string contentControlThemeFontFamily)
	{
		Application.Current.Resources["ContentControlThemeFontFamily"] = contentControlThemeFontFamily;
	}

	public void SetCompactSpacing(bool useCompactSpacing)
	{
		var listItemHeight = useCompactSpacing ? 24 : 36;
		var navigationViewItemOnLeftMinHeight = useCompactSpacing ? 20 : 32;

		Application.Current.Resources["ListItemHeight"] = listItemHeight;
		Application.Current.Resources["NavigationViewItemOnLeftMinHeight"] = navigationViewItemOnLeftMinHeight;
	}

	public void ApplyResources()
	{
		// Get the index of the current theme
		var selTheme = ThemeHelper.RootTheme;

		// Toggle between the themes to force reload the resource styles
		ThemeHelper.RootTheme = ElementTheme.Dark;
		ThemeHelper.RootTheme = ElementTheme.Light;

		// Restore the theme to the correct theme
		ThemeHelper.RootTheme = selTheme;
	}
}
