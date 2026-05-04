// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Settings;

public sealed partial class Settings
{
	[GeneratedSettingsProperty(DefaultValue = true)]
	public partial bool ShowStatusCenterTeachingTip { get; set; }

	[GeneratedSettingsProperty(DefaultValue = true)]
	public partial bool ShowBackgroundRunningNotification { get; set; }

	[GeneratedSettingsProperty(DefaultValue = false)]
	public partial bool RestoreTabsOnStartup { get; set; }
}
