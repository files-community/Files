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

	public static IAsyncOperation<IReadOnlyList<LibraryInfo>> GetLibrariesAsync() => ServerAsync.Run(token => InitializationCache.Libraries.GetAsync().WaitAsync(token));

	public static IAsyncOperation<IReadOnlyList<PinnedFolderInfo>> GetPinnedFoldersAsync() => ServerAsync.Run(token => InitializationCache.PinnedFolders.GetAsync().WaitAsync(token));

	public static void InvalidatePinnedFolders()
	{
		InitializationCache.PinnedFolders.Invalidate();
		ServerEvents.Notify(ServerChangeNames.PinnedFolders);
	}

	public void Dispose()
	{
		if (Interlocked.Exchange(ref disposed, 1) == 0)
			ServerHost.RemoveClient(ClientId);
		GC.SuppressFinalize(this);
	}
}
