using System.Drawing;

namespace Files.Shared.Services;

public interface IResourcesService
{
	void ApplyResources();
	void SetAppThemeBackgroundColor(Color appThemeBackgroundColor);
	void SetAppThemeAddressBarBackgroundColor(Color appThemeAddressBarBackgroundColor);
	void SetAppThemeSidebarBackgroundColor(Color appThemeSidebarBackgroundColor);
	void SetAppThemeFileAreaBackgroundColor(Color appThemeFileAreaBackgroundColor);
	void SetAppThemeFontFamily(string contentControlThemeFontFamily);
	void SetCompactSpacing(bool useCompactSpacing);
}