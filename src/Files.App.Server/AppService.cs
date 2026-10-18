// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;

namespace Files.App.Server;

public sealed partial class AppService : IDisposable
{
	private readonly long clientId;
	private int disposed;

	public AppService(int processId)
	{
		clientId = ServerHost.AddClient(processId);
	}

	~AppService() => Dispose();

	public event EventHandler<string> Changed
	{
		add => ServerEvents.Add(clientId, value);
		remove => ServerEvents.Remove(clientId, value);
	}

	public static int CloudProviderGroupCount => InitializationCache.CloudProviders.Length;

	public void SetWindowHandle(long handle) => ServerHost.SetWindow(clientId, handle);

	public static IAsyncAction InitializeAsync() => ServerAsync.Run(token => StartupServices.InitializeAsync().WaitAsync(token));

	public static IAsyncOperation<IReadOnlyList<ShellNewEntryInfo>> GetShellNewEntriesAsync() => ServerAsync.Run(token => InitializationCache.ShellNew.GetAsync().WaitAsync(token));

	public static IAsyncOperation<ShellNewEntryInfo?> GetShellNewEntryAsync(string extension) => ServerAsync.Run(async token =>
	{
		var entry = await Task.Run(() => ShellNewMenuHelper.GetNewContextMenuEntryForType(extension), token);
		return entry is null ? null : new ShellNewEntryInfo(entry);
	});

	public static IAsyncOperation<IReadOnlyList<string>> GetWslDistributionsAsync() => ServerAsync.Run(token => InitializationCache.Wsl.GetAsync().WaitAsync(token));

	public static IAsyncOperation<IReadOnlyList<LibraryInfo>> GetLibrariesAsync() => ServerAsync.Run(token => InitializationCache.Libraries.GetAsync().WaitAsync(token));

	public static IAsyncOperation<IReadOnlyList<PinnedFolderInfo>> GetPinnedFoldersAsync() => ServerAsync.Run(token => InitializationCache.PinnedFolders.GetAsync().WaitAsync(token));

	public static void InvalidatePinnedFolders()
	{
		InitializationCache.PinnedFolders.Invalidate();
		JumpListCatalog.Invalidate();
		ServerEvents.Notify("PinnedFolders");
	}

	public static IAsyncOperation<IReadOnlyList<CloudProviderInfo>> GetCloudProvidersAsync(int group)
	{
		if ((uint)group >= InitializationCache.CloudProviders.Length)
			throw new ArgumentOutOfRangeException(nameof(group));
		return ServerAsync.Run(token => InitializationCache.CloudProviders[group].GetAsync().WaitAsync(token));
	}

	public static IAsyncOperation<IReadOnlyList<TaggedFileInfo>> GetTaggedFilesAsync() => ServerAsync.Run(token => TagCatalog.Items.GetAsync().WaitAsync(token));

	public static IAsyncOperation<bool> CheckReleaseNotesAsync() => ServerAsync.Run(token => UpdateCatalog.ReleaseNotes.GetAsync().WaitAsync(token));

	public static IAsyncOperation<bool> CheckSideloadUpdatesAsync() => ServerAsync.Run(token => UpdateCatalog.SideloadUpdate.GetAsync().WaitAsync(token));

	public static IAsyncOperation<IReadOnlyList<Windows.Services.Store.StorePackageUpdate>> GetStoreUpdatesAsync() => ServerAsync.Run(token => UpdateCatalog.StoreUpdates.GetAsync().WaitAsync(token));

	public static IAsyncAction UpdateLauncherAsync() => ServerAsync.Run(token => UpdateCatalog.UpdateLauncherAsync().WaitAsync(token));

	public static void InvalidateUpdates() => UpdateCatalog.Invalidate();

	public static IAsyncOperation<ContextMenuSession?> GetContextMenuAsync([ReadOnlyArray] string[] paths, uint flags) => ServerAsync.Run(async token =>
	{
		var menu = await ContextMenu.GetContextMenuForFiles(paths, flags);
		return menu is null ? null : new ContextMenuSession(menu);
	});

	public static IAsyncOperation<Windows.Storage.Streams.IBuffer> GetGenericIconAsync(string extension, bool folder, int size) => ServerAsync.Run(token => IconCache.GetGenericAsync(extension, folder, size).WaitAsync(token));

	public static IAsyncOperation<Windows.Storage.Streams.IBuffer> GetIconAsync(string path, int size, bool folder, uint options, bool overlay) => ServerAsync.Run(token => IconCache.GetAsync(path, size, folder, options, overlay).WaitAsync(token));

	public static IAsyncOperation<IReadOnlyList<string>> GetDriveIdsAsync() => ServerAsync.Run(token => DriveCatalog.GetIdsAsync().WaitAsync(token));

	public static IAsyncOperation<DriveItemInfo?> GetDriveAsync(string id) => ServerAsync.Run(token => DriveCatalog.GetAsync(id).WaitAsync(token));

	public static IAsyncOperation<DrivePropertiesInfo> GetDrivePropertiesAsync(string path, bool cloud) => ServerAsync.Run(token => DriveCatalog.GetPropertiesAsync(path, cloud).WaitAsync(token));

	public static IAsyncOperation<IReadOnlyList<ShellLocationInfo>> GetRecentItemsAsync(bool folders) => ServerAsync.Run(token => (folders ? LocationCatalog.RecentFolders : LocationCatalog.RecentFiles).GetAsync().WaitAsync(token));

	public static IAsyncOperation<IReadOnlyList<ShellLocationInfo>> GetNetworkLocationsAsync(bool shortcuts) => ServerAsync.Run(token => (shortcuts ? LocationCatalog.NetworkShortcuts : LocationCatalog.NetworkComputers).GetAsync().WaitAsync(token));

	public static IAsyncOperation<bool> HasRecycleBinItemsAsync() => ServerAsync.Run(token => RecycleBinCatalog.HasItemsAsync().WaitAsync(token));

	public static IAsyncOperation<RecycleBinInfo> QueryRecycleBinAsync(string path) => ServerAsync.Run(token => RecycleBinCatalog.QueryAsync(path).WaitAsync(token));

	public static SettingsValue[] GetSettings(string fileName) => SettingsStore.Get(fileName).ReadValues();

	public static bool SetSetting(string fileName, string key, [ReadOnlyArray] SettingsValue[] value) => SettingsStore.Get(fileName).Set(key, value, false);

	public static bool RemoveSetting(string fileName, string key) => SettingsStore.Get(fileName).Set(key, [], true);

	public static bool ImportSettings(string fileName, [ReadOnlyArray] SettingsValue[] values) => SettingsStore.Get(fileName).Import(values);

	public static IAsyncAction RefreshJumpListAsync() => ServerAsync.Run(token => JumpListCatalog.RefreshAsync().WaitAsync(token));

	public static IAsyncAction AddJumpListFolderAsync(string path) => ServerAsync.Run(token => JumpListCatalog.AddAsync(path).WaitAsync(token));

	public static IAsyncAction RemoveJumpListFolderAsync(string path) => ServerAsync.Run(token => JumpListCatalog.RemoveAsync(path).WaitAsync(token));

	public static IAsyncOperation<IReadOnlyList<string>> GetJumpListFoldersAsync() => ServerAsync.Run(token => JumpListCatalog.GetAsync().WaitAsync(token));

	public static void BeginUpdate() => ServerHost.BeginUpdate();

	public static void EndUpdate() => ServerHost.EndUpdate();

	public void Dispose()
	{
		if (Interlocked.Exchange(ref disposed, 1) == 0)
			ServerHost.RemoveClient(clientId);
		GC.SuppressFinalize(this);
	}
}
