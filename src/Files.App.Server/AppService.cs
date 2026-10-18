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

	public static int CloudProviderGroupCount => InitializationCache.CloudProviders.Length;

	public void SetWindowHandle(long handle) => ServerHost.SetWindow(clientId, handle);

	public static IAsyncAction InitializeAsync() => AsyncInfo.Run(token => InitializationCache.InitializeAsync().WaitAsync(token));

	public static IAsyncOperation<IReadOnlyList<ShellNewEntryInfo>> GetShellNewEntriesAsync() => AsyncInfo.Run(token => InitializationCache.ShellNew.GetAsync().WaitAsync(token));

	public static IAsyncOperation<ShellNewEntryInfo?> GetShellNewEntryAsync(string extension) => AsyncInfo.Run(async token =>
	{
		var entry = await Task.Run(() => ShellNewMenuHelper.GetNewContextMenuEntryForType(extension), token);
		return entry is null ? null : new ShellNewEntryInfo(entry);
	});

	public static IAsyncOperation<IReadOnlyList<string>> GetWslDistributionsAsync() => AsyncInfo.Run(token => InitializationCache.Wsl.GetAsync().WaitAsync(token));

	public static IAsyncOperation<IReadOnlyList<LibraryInfo>> GetLibrariesAsync() => AsyncInfo.Run(token => InitializationCache.Libraries.GetAsync().WaitAsync(token));

	public static IAsyncOperation<IReadOnlyList<PinnedFolderInfo>> GetPinnedFoldersAsync() => AsyncInfo.Run(token => InitializationCache.PinnedFolders.GetAsync().WaitAsync(token));

	public static void InvalidatePinnedFolders() => InitializationCache.PinnedFolders.Invalidate();

	public static IAsyncOperation<IReadOnlyList<CloudProviderInfo>> GetCloudProvidersAsync(int group)
	{
		if ((uint)group >= InitializationCache.CloudProviders.Length)
			throw new ArgumentOutOfRangeException(nameof(group));
		return AsyncInfo.Run(token => InitializationCache.CloudProviders[group].GetAsync().WaitAsync(token));
	}

	public static void BeginUpdate() => ServerHost.BeginUpdate();

	public static void EndUpdate() => ServerHost.EndUpdate();

	public void Dispose()
	{
		if (Interlocked.Exchange(ref disposed, 1) == 0)
			ServerHost.RemoveClient(clientId);
		GC.SuppressFinalize(this);
	}
}
