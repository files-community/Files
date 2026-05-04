// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Settings;

public sealed partial class Settings
{
	[GeneratedSettingsProperty(DefaultValue = false)]
	public partial bool IsInfoPaneEnabled { get; set; }

	[GeneratedSettingsProperty(DefaultValue = 300d, GetValueCallback = nameof(GetInfoPaneSize))]
	public partial double HorizontalSizePx { get; set; }

	[GeneratedSettingsProperty(DefaultValue = 250d, GetValueCallback = nameof(GetInfoPaneSize))]
	public partial double VerticalSizePx { get; set; }

	[GeneratedSettingsProperty(DefaultValue = 1d, GetValueCallback = nameof(GetMediaVolume))]
	public partial double MediaVolume { get; set; }

	[GeneratedSettingsProperty(DefaultValue = InfoPaneTabs.Details)]
	public partial InfoPaneTabs SelectedTab { get; set; }

	private static double GetInfoPaneSize(double value)
	{
		return Math.Max(100d, value);
	}

	private static double GetMediaVolume(double value)
	{
		return Math.Min(Math.Max(value, 0d), 1d);
	}
}
