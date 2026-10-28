// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.Foundation;

namespace Files.App.Server;

public sealed partial class AppService : IDisposable
{
	private int disposed;

	internal long ClientId { get; }
	internal bool IsDisposed => disposed != 0;

	public bool CanRestorePreviousSession { get; }

	public AppService(int processId)
	{
		(ClientId, CanRestorePreviousSession) = ServerHost.AddClient(processId);
	}

	~AppService() => Dispose();

	public event EventHandler<string> Changed
	{
		add => ServerEvents.Add(ClientId, value);
		remove => ServerEvents.Remove(ClientId, value);
	}

	public void SetWindowHandle(long handle) => ServerHost.SetWindow(ClientId, handle);

	public static IAsyncAction InitializeAsync() => ServerAsync.Run(token => StartupServices.InitializeAsync().WaitAsync(token));

	public static int CloudProviderGroupCount => InitializationCache.CloudProviders.Length;

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
		ServerEvents.Notify(ServerChangeNames.PinnedFolders);
	}

	public static IAsyncOperation<IReadOnlyList<CloudProviderInfo>> GetCloudProvidersAsync(int group)
	{
		if ((uint)group >= InitializationCache.CloudProviders.Length)
			throw new ArgumentOutOfRangeException(nameof(group));
		return ServerAsync.Run(token => InitializationCache.CloudProviders[group].GetAsync().WaitAsync(token));
	}

	public void Dispose()
	{
		if (Interlocked.Exchange(ref disposed, 1) == 0)
			ServerHost.RemoveClient(ClientId);
		GC.SuppressFinalize(this);
	}
}
