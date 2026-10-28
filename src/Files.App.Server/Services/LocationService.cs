// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.Foundation;

namespace Files.App.Server;

public sealed partial class LocationService
{
	private LocationService() { }

	public static IAsyncOperation<IReadOnlyList<LibraryInfo>> GetLibrariesAsync() => ServerAsync.Run(token => InitializationCache.Libraries.GetAsync().WaitAsync(token));

	public static IAsyncOperation<IReadOnlyList<ShellLocationInfo>> GetRecentItemsAsync(bool folders) => ServerAsync.Run(token => (folders ? LocationCatalog.RecentFolders : LocationCatalog.RecentFiles).GetAsync().WaitAsync(token));

	public static IAsyncOperation<IReadOnlyList<ShellLocationInfo>> GetNetworkLocationsAsync(bool shortcuts) => ServerAsync.Run(token => (shortcuts ? LocationCatalog.NetworkShortcuts : LocationCatalog.NetworkComputers).GetAsync().WaitAsync(token));
}
