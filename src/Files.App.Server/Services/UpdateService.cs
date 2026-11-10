// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.Foundation;
using Windows.Services.Store;

namespace Files.App.Server;

public sealed partial class UpdateService
{
	private UpdateService() { }

	public static IAsyncOperation<bool> CheckReleaseNotesAsync() => ServerAsync.Run(token => UpdateCatalog.ReleaseNotes.GetAsync().WaitAsync(token));

	public static IAsyncOperation<bool> CheckSideloadUpdatesAsync() => ServerAsync.Run(token => UpdateCatalog.SideloadUpdate.GetAsync().WaitAsync(token));

	public static IAsyncOperation<IReadOnlyList<StorePackageUpdate>> GetStoreUpdatesAsync() => ServerAsync.Run(token => UpdateCatalog.StoreUpdates.GetAsync().WaitAsync(token));

	public static IAsyncAction UpdateLauncherAsync() => ServerAsync.Run(token => UpdateCatalog.UpdateLauncherAsync().WaitAsync(token));

	public static void InvalidateUpdates() => UpdateCatalog.Invalidate();

	public static void BeginUpdate() => ServerHost.BeginUpdate();

	public static void EndUpdate() => ServerHost.EndUpdate();
}
