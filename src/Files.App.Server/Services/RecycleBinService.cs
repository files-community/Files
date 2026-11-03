// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.Foundation;

namespace Files.App.Server;

public sealed partial class RecycleBinService
{
	private readonly AppService client;

	internal RecycleBinService(AppService client) => this.client = client;

	public RecycleBinChange[] GetChanges() => RecycleBinCatalog.GetChanges(client.ClientId);

	public static IAsyncOperation<bool> HasRecycleBinItemsAsync() => ServerAsync.Run(token => RecycleBinCatalog.HasItemsAsync().WaitAsync(token));

	public static IAsyncOperation<RecycleBinInfo> QueryRecycleBinAsync(string path) => ServerAsync.Run(token => RecycleBinCatalog.QueryAsync(path).WaitAsync(token));
}
