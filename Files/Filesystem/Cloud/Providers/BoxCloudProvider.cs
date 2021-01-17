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
        public async Task<IEnumerable<CloudProvider>> DetectAsync()
        {
            try
            {
                var infoPath = @"Box\Box\data\shell\sync_root_folder.txt";
                var configPath = Path.Combine(UserDataPaths.GetDefault().LocalAppData, infoPath);
                var configFile = await StorageFile.GetFileFromPathAsync(configPath);
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