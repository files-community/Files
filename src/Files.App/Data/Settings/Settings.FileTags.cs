// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Settings;

public sealed partial class Settings
{
	[GeneratedSettingsProperty]
	public partial List<TagViewModel>? FileTagList { get; set; }
}
