// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.Foundation;

namespace Files.App.Server;

public sealed partial class PinnedFoldersService
{
	private readonly AppService client;

	internal PinnedFoldersService(AppService client) => this.client = client;

	public void BeginUpdate()
	{
		ObjectDisposedException.ThrowIf(client.IsDisposed, client);
		ServerHost.BeginPinnedFoldersUpdate(client.ClientId);
	}

	public void EndUpdate() => ServerEvents.EndPinnedFoldersUpdate(client.ClientId);

	public static IAsyncOperation<IReadOnlyList<PinnedFolderInfo>> GetPinnedFoldersAsync() => ServerAsync.Run(token => InitializationCache.PinnedFolders.GetAsync().WaitAsync(token));

	public static void InvalidatePinnedFolders()
	{
		InitializationCache.PinnedFolders.Invalidate();
		JumpListCatalog.Invalidate();
		ServerEvents.Notify(ServerChangeNames.PinnedFolders);
	}
}
