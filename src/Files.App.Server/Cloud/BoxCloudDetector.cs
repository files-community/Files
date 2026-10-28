// Copyright (c) Files Community
// Licensed under the MIT License.

using System.IO;
using Windows.Storage;

namespace Files.App.Server.Cloud
{
	/// <summary>
	/// Provides an utility for Box Cloud detection.
	/// </summary>
	internal sealed class BoxCloudDetector : AbstractCloudDetector
	{
		protected override async IAsyncEnumerable<ICloudProvider> GetProviders()
		{
			string configPathBoxDrive = Path.Combine(UserDataPaths.GetDefault().LocalAppData, @"Box\Box\data\shell\sync_root_folder.txt");

			string configPathBoxSync = Path.Combine(UserDataPaths.GetDefault().LocalAppData, @"Box Sync\sync_root_folder.txt");

			var configPath = File.Exists(configPathBoxDrive) ? configPathBoxDrive : configPathBoxSync;
			if (File.Exists(configPath))
			{
				string syncPath = await File.ReadAllTextAsync(configPath);

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
