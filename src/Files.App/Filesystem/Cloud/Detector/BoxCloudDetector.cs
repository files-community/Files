// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Shared.Cloud;
using System.IO;
using Windows.Storage;

namespace Files.App.Filesystem.Cloud
{
	public class BoxCloudDetector : AbstractCloudDetector
	{
		protected override async IAsyncEnumerable<ICloudProvider> GetProviders()
		{
			string configPathBoxDrive = Path.Combine(UserDataPaths.GetDefault().LocalAppData, @"Box\Box\data\shell\sync_root_folder.txt");
			string configPathBoxSync = Path.Combine(UserDataPaths.GetDefault().LocalAppData, @"Box Sync\sync_root_folder.txt");

			StorageFile configFile = await FilesystemTasks.Wrap(() => StorageFile.GetFileFromPathAsync(configPathBoxDrive).AsTask());
			if (configFile is null)
			{
				configFile = await FilesystemTasks.Wrap(() => StorageFile.GetFileFromPathAsync(configPathBoxSync).AsTask());
			}
			if (configFile is not null)
			{
				string syncPath = await FileIO.ReadTextAsync(configFile);
				if (!string.IsNullOrEmpty(syncPath))
				{
					yield return new CloudProvider(CloudProviders.Box)
					{
						Name = "Box",
						SyncFolder = syncPath,
					};
				}
			}
		}
	}
}