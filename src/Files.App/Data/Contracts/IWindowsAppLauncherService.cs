// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.Contracts
{
	public interface IWindowsAppLauncherService
	{
		Task<bool> LaunchStorageSensePolicySettingsAsync();

		Task<bool> LaunchProgramCompatibilityTroubleshooterAsync(string path);

		Task<bool> LaunchApplicationAsync(string path, string arguments, string workingDirectory);
	}
}
