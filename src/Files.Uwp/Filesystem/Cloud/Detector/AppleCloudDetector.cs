using Files.Shared.Cloud;
using System;
using System.Collections.Generic;
using System.IO;
using Windows.Storage;

namespace Files.Uwp.Filesystem.Cloud
{
    public class AppleCloudDetector : AbstractCloudDetector
    {
        protected override async IAsyncEnumerable<ICloudProvider> GetProviders()
        {
            string userPath = UserDataPaths.GetDefault().Profile;
            string iCloudPath = Path.Combine(userPath, "iCloudDrive");
            var driveFolder = await StorageFolder.GetFolderFromPathAsync(iCloudPath);

            yield return new CloudProvider(CloudProviders.AppleCloud)
            {
                Name = "iCloud",
                SyncFolder = driveFolder.Path,
            };
        }
    }
}