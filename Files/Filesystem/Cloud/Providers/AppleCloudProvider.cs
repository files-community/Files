using Files.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace Files.Filesystem.Cloud.Providers
{
    public class AppleCloudProvider : ICloudProviderDetector
    {
        public async Task DetectAsync(List<CloudProvider> cloudProviders)
        {
            try
            {
                var userPath = UserDataPaths.GetDefault().Profile;
                var iCloudPath = "iCloudDrive";
                var driveFolder = await StorageFolder.GetFolderFromPathAsync(Path.Combine(userPath, iCloudPath));

                cloudProviders.Add(new CloudProvider()
                {
                    ID = CloudProviders.AppleCloud,
                    Name = "iCloud",
                    SyncFolder = driveFolder.Path
                });
            }
            catch
            {
                // Not detected
            }
        }
    }
}