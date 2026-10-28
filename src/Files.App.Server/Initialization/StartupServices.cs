// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Server;

internal static class StartupServices
{
	internal static async Task InitializeAsync()
	{
		await Task.WhenAll(
			SafetyExtensions.IgnoreExceptions(InitializationCache.InitializeAsync, Program.Logger),
			SafetyExtensions.IgnoreExceptions(TagCatalog.InitializeAsync, Program.Logger),
			SafetyExtensions.IgnoreExceptions(UpdateCatalog.InitializeAsync, Program.Logger));
	}

	internal static void Stop()
	{
		UpdateCatalog.Stop();
		SettingsStore.Stop();
	}
}
