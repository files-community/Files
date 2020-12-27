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
                var infoPath = @"Box\Box\data\shell\sync_root_folder.txt";
                var configPath = Path.Combine(UserDataPaths.GetDefault().LocalAppData, infoPath);
                var configFile = await StorageFile.GetFileFromPathAsync(configPath);
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
            catch
            {
                // Not detected
            }
        }
    }
}