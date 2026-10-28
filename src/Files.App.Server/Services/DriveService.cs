// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.Foundation;

namespace Files.App.Server;

public sealed partial class DriveService
{
	private DriveService() { }

	public static int CloudProviderGroupCount => InitializationCache.CloudProviders.Length;

	public static IAsyncOperation<IReadOnlyList<string>> GetDriveIdsAsync() => ServerAsync.Run(token => DriveCatalog.GetIdsAsync().WaitAsync(token));

	public static IAsyncOperation<DriveItemInfo?> GetDriveAsync(string id) => ServerAsync.Run(token => DriveCatalog.GetAsync(id).WaitAsync(token));

	public static IAsyncOperation<DrivePropertiesInfo> GetDrivePropertiesAsync(string path, bool cloud) => ServerAsync.Run(token => DriveCatalog.GetPropertiesAsync(path, cloud).WaitAsync(token));

	public static IAsyncOperation<IReadOnlyList<string>> GetWslDistributionsAsync() => ServerAsync.Run(token => InitializationCache.Wsl.GetAsync().WaitAsync(token));

	public static IAsyncOperation<IReadOnlyList<CloudProviderInfo>> GetCloudProvidersAsync(int group)
	{
		if ((uint)group >= InitializationCache.CloudProviders.Length)
			throw new ArgumentOutOfRangeException(nameof(group));
		return ServerAsync.Run(token => InitializationCache.CloudProviders[group].GetAsync().WaitAsync(token));
	}
}
