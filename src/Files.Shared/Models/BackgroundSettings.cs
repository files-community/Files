// Copyright (c) Files Community
// Licensed under the MIT License.

using System;

namespace Files.Shared;

public static class BackgroundSettings
{
	// Replaced by CD with the release environment name; Dev builds keep the placeholder
	private const string AppEnvironmentName = "cd_app_env_placeholder";

	public const string CloseToBackgroundKey = "CloseToBackground";
	public static readonly bool CloseToBackgroundDefault = !IsDevEnvironment();
	public const string ShowSystemTrayIconKey = "ShowSystemTrayIcon";
	public const bool ShowSystemTrayIconDefault = true;

	private static bool IsDevEnvironment()
		=> AppEnvironmentName.StartsWith("cd_", StringComparison.Ordinal) ||
			AppEnvironmentName.Equals("Dev", StringComparison.OrdinalIgnoreCase);
}
