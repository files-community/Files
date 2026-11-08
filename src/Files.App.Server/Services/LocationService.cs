// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.Foundation;

namespace Files.App.Server;

public sealed partial class LocationService
{
	private LocationService() { }

	public static IAsyncOperation<IReadOnlyList<LibraryInfo>> GetLibrariesAsync() => ServerAsync.Run(token => InitializationCache.Libraries.GetAsync().WaitAsync(token));

	public static IAsyncOperation<IReadOnlyList<ShellLocationInfo>> GetRecentItemsAsync(bool folders, bool refresh) => ServerAsync.Run(token =>
	{
		var cache = folders ? LocationCatalog.RecentFolders : LocationCatalog.RecentFiles;
		if (refresh)
			cache.Invalidate();
		return cache.GetAsync().WaitAsync(token);
	});

	public static IAsyncOperation<IReadOnlyList<ShellLocationInfo>> GetNetworkLocationsAsync(bool shortcuts, bool refresh) => ServerAsync.Run(token =>
	{
		var cache = shortcuts ? LocationCatalog.NetworkShortcuts : LocationCatalog.NetworkComputers;
		if (refresh)
			cache.Invalidate();
		return cache.GetAsync().WaitAsync(token);
	});
}
