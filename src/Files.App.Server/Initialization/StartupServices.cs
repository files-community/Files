// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Server;

internal static class StartupServices
{
	private static readonly Lazy<Task> menuInitialization = new(() => SafetyExtensions.IgnoreExceptions(ContextMenu.WarmUpQueryContextMenuAsync, Program.Logger));

	internal static async Task InitializeAsync()
	{
		StartupWatchers.Initialize();
		await Task.WhenAll(
			SafetyExtensions.IgnoreExceptions(InitializationCache.InitializeAsync, Program.Logger),
			SafetyExtensions.IgnoreExceptions(TagCatalog.InitializeAsync, Program.Logger),
			SafetyExtensions.IgnoreExceptions(LocationCatalog.RecentFiles.GetAsync, Program.Logger),
			SafetyExtensions.IgnoreExceptions(LocationCatalog.RecentFolders.GetAsync, Program.Logger),
			SafetyExtensions.IgnoreExceptions(LocationCatalog.NetworkShortcuts.GetAsync, Program.Logger),
			SafetyExtensions.IgnoreExceptions(UpdateCatalog.InitializeAsync, Program.Logger),
			SafetyExtensions.IgnoreExceptions(JumpListCatalog.RefreshAsync, Program.Logger),
			WarmDrivesAsync(),
			menuInitialization.Value);
	}

	private static async Task WarmDrivesAsync()
	{
		var ids = await SafetyExtensions.IgnoreExceptions(DriveCatalog.GetIdsAsync, Program.Logger);
		foreach (var id in ids ?? Array.Empty<string>())
			_ = SafetyExtensions.IgnoreExceptions(() => DriveCatalog.GetAsync(id), Program.Logger);
	}

	internal static void Stop()
	{
		UpdateCatalog.Stop();
		SettingsStore.Stop();
		StartupWatchers.Dispose();
		DriveCatalog.Stop();
	}
}
