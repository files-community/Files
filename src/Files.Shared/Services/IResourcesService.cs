using System.Drawing;

namespace Files.Shared.Services;

/// <summary>
/// Contains methods related to modifying various app theme resources
/// </summary>
public interface IResourcesService
{
	/// <summary>
	/// Applies updated resource styles
	/// </summary>
	void ApplyResources();

	/// <summary>
	/// Overrides the XAML resource for App.Theme.BackgroundBrush
	/// </summary>
	/// <param name="appThemeBackgroundColor"></param>
	void SetAppThemeBackgroundColor(Color appThemeBackgroundColor);

	/// <summary>
	/// Overrides the XAML resource for App.Theme.AddressBar.BackgroundBrush
	/// </summary>
	/// <param name="appThemeAddressBarBackgroundColor"></param>
	void SetAppThemeAddressBarBackgroundColor(Color appThemeAddressBarBackgroundColor);

	/// <summary>
	/// Overrides the XAML resource for App.Theme.Sidebar.BackgroundBrush
	/// </summary>
	/// <param name="appThemeSidebarBackgroundColor"></param>
	void SetAppThemeSidebarBackgroundColor(Color appThemeSidebarBackgroundColor);

	/// <summary>
	/// Overrides the XAML resource for App.Theme.FileArea.BackgroundBrush
	/// </summary>
	/// <param name="appThemeFileAreaBackgroundColor"></param>
	void SetAppThemeFileAreaBackgroundColor(Color appThemeFileAreaBackgroundColor);

	/// <summary>
	/// Overrides the XAML resource for ContentControlThemeFontFamily
	/// </summary>
	/// <param name="contentControlThemeFontFamily"></param>
	void SetAppThemeFontFamily(string contentControlThemeFontFamily);

	/// <summary>
	/// Overrides the XAML resource for the list view item height
	/// </summary>
	/// <param name="useCompactSpacing"></param>
	void SetCompactSpacing(bool useCompactSpacing);
}