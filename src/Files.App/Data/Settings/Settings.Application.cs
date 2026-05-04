// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Settings;

public sealed partial class Settings
{
	[GeneratedSettingsProperty(DefaultValue = false)]
	public partial bool HasClickedReviewPrompt { get; set; }

	[GeneratedSettingsProperty(DefaultValue = false)]
	public partial bool HasClickedSponsorPrompt { get; set; }

	[GeneratedSettingsProperty(DefaultValue = true)]
	public partial bool ShowRunningAsAdminPrompt { get; set; }

	[GeneratedSettingsProperty(DefaultValue = true)]
	public partial bool ShowDataStreamsAreHiddenPrompt { get; set; }
}
