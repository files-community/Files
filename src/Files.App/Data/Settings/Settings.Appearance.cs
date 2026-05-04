// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Files.App.Data.Settings;

public sealed partial class Settings
{
	[GeneratedSettingsProperty(DefaultValue = 255d, GetValueCallback = nameof(GetSidebarWidth))]
	public partial double SidebarWidth { get; set; }

	[GeneratedSettingsProperty(DefaultValue = true)]
	public partial bool IsSidebarOpen { get; set; }

	[GeneratedSettingsProperty(DefaultValue = "Default")]
	public partial string AppThemeMode { get; set; }

	[GeneratedSettingsProperty(DefaultValue = "#00000000")]
	public partial string AppThemeBackgroundColor { get; set; }

	[GeneratedSettingsProperty(DefaultValue = "")]
	public partial string AppThemeAddressBarBackgroundColor { get; set; }

	[GeneratedSettingsProperty(DefaultValue = "")]
	public partial string AppThemeToolbarBackgroundColor { get; set; }

	[GeneratedSettingsProperty(DefaultValue = "")]
	public partial string AppThemeSidebarBackgroundColor { get; set; }

	[GeneratedSettingsProperty(DefaultValue = "")]
	public partial string AppThemeFileAreaBackgroundColor { get; set; }

	[GeneratedSettingsProperty(DefaultValue = "")]
	public partial string AppThemeFileAreaSecondaryBackgroundColor { get; set; }

	[GeneratedSettingsProperty(DefaultValue = "")]
	public partial string AppThemeInfoPaneBackgroundColor { get; set; }

	[GeneratedSettingsProperty(DefaultValueCallback = nameof(GetDefaultAppThemeFontFamily))]
	public partial string AppThemeFontFamily { get; set; }

	[GeneratedSettingsProperty(DefaultValue = BackdropMaterialType.MicaAlt)]
	public partial BackdropMaterialType AppThemeBackdropMaterial { get; set; }

	[GeneratedSettingsProperty(DefaultValue = "")]
	public partial string AppThemeBackgroundImageSource { get; set; }

	[GeneratedSettingsProperty(DefaultValue = Stretch.UniformToFill)]
	public partial Stretch AppThemeBackgroundImageFit { get; set; }

	[GeneratedSettingsProperty(DefaultValue = 1f)]
	public partial float AppThemeBackgroundImageOpacity { get; set; }

	[GeneratedSettingsProperty(DefaultValue = VerticalAlignment.Center)]
	public partial VerticalAlignment AppThemeBackgroundImageVerticalAlignment { get; set; }

	[GeneratedSettingsProperty(DefaultValue = HorizontalAlignment.Center)]
	public partial HorizontalAlignment AppThemeBackgroundImageHorizontalAlignment { get; set; }

	[GeneratedSettingsProperty(DefaultValue = true)]
	public partial bool ShowToolbar { get; set; }

	[GeneratedSettingsProperty(DefaultValue = true)]
	public partial bool ShowStatusBar { get; set; }

	[GeneratedSettingsProperty(DefaultValue = true)]
	public partial bool ShowTabActions { get; set; }

	[GeneratedSettingsProperty(DefaultValue = false)]
	public partial bool ShowShelfPaneToggleButton { get; set; }

	[GeneratedSettingsProperty(DefaultValue = StatusCenterVisibility.Always)]
	public partial StatusCenterVisibility StatusCenterVisibility { get; set; }

	[GeneratedSettingsProperty]
	public partial Dictionary<string, List<ToolbarItemSettingsEntry>>? CustomToolbarItems { get; set; }

	[GeneratedSettingsProperty]
	public partial Dictionary<string, List<string>>? LastKnownToolbarDefaults { get; set; }

	private static double GetSidebarWidth(double value)
	{
		return Math.Min(Math.Max(value, Constants.UI.MinimumSidebarWidth), 500d);
	}

	private static string GetDefaultAppThemeFontFamily()
	{
		return Constants.Appearance.StandardFont;
	}
}
