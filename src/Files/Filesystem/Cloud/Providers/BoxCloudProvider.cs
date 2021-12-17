using Files.Common;
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
        public async Task<IList<CloudProvider>> DetectAsync()
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
                        return new[] { new CloudProvider()
                            {
                                ID = CloudProviders.Box,
                                Name = "Box",
                                SyncFolder = syncPath
                            }
                        };
                    }
                }

                return Array.Empty<CloudProvider>();
            }
            catch
            {
                // Not detected
                return Array.Empty<CloudProvider>();
            }
        }
    }
}