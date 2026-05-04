// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Settings;

public sealed partial class Settings
{
	[GeneratedSettingsProperty(DefaultValue = OpenInIDEOption.GitRepos)]
	public partial OpenInIDEOption OpenInIDEOption { get; set; }

	[GeneratedSettingsProperty(DefaultValueCallback = nameof(GetDefaultIDEPath))]
	public partial string IDEPath { get; set; }

	[GeneratedSettingsProperty(DefaultValueCallback = nameof(GetDefaultIDEName))]
	public partial string IDEName { get; set; }

	private static string GetDefaultIDEPath()
	{
		return SoftwareHelpers.IsVSCodeInstalled()
			? "code"
			: string.Empty;
	}

	private static string GetDefaultIDEName()
	{
		return SoftwareHelpers.IsVSCodeInstalled()
			? Strings.VisualStudioCode.GetLocalizedResource()
			: string.Empty;
	}
}
