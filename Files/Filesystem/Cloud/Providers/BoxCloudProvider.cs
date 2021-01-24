using Files.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace Files.Filesystem.Cloud.Providers
{
    public class BoxCloudProvider : ICloudProviderDetector
    {
        public async Task DetectAsync(List<CloudProvider> cloudProviders)
        {
            try
            {
                var infoPathBoxDrive = @"Box\Box\data\shell\sync_root_folder.txt";
                var configPathBoxDrive = Path.Combine(UserDataPaths.GetDefault().LocalAppData, infoPathBoxDrive);
                var infoPathBoxSync = @"Box Sync\sync_root_folder.txt";
                var configPathBoxSync = Path.Combine(UserDataPaths.GetDefault().LocalAppData, infoPathBoxSync);

                StorageFile configFile = await FilesystemTasks.Wrap(() => StorageFile.GetFileFromPathAsync(configPathBoxDrive).AsTask());
                if (configFile == null)
                {
                    configFile = await FilesystemTasks.Wrap(() => StorageFile.GetFileFromPathAsync(configPathBoxSync).AsTask());
                }
                if (configFile != null)
                {
                    var syncPath = await FileIO.ReadTextAsync(configFile);

                    if (!string.IsNullOrEmpty(syncPath))
                    {
                        cloudProviders.Add(new CloudProvider()
                        {
                            ID = CloudProviders.Box,
                            Name = "Box",
                            SyncFolder = syncPath
                        });
                    }
                }
            }
            catch
            {
                // Not detected
            }
        }
    }
}