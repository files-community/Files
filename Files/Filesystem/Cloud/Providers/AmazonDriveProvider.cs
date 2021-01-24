using Files.Enums;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Files.Filesystem.Cloud.Providers
{
    public class AmazonDriveProvider : ICloudProviderDetector
    {
        public async Task<IList<CloudProvider>> DetectAsync()
        {
            try
            {
                using var key = Registry.ClassesRoot.OpenSubKey(@"CLSID\{9B57F475-CCB0-4C85-88A9-2AA9A6C0809A}\Instance\InitPropertyBag");
                var syncedFolder = (string)key?.GetValue("TargetFolderPath");

                if (syncedFolder == null)
                {
                    return Array.Empty<CloudProvider>();
                }

                return new[] { new CloudProvider()
                    {
                        ID = CloudProviders.AmazonDrive,
                        Name = "Amazon Drive",
                        SyncFolder = syncedFolder
                    }
                };
            }
            catch
            {
                // Not detected
                return Array.Empty<CloudProvider>();
            }
        }
    }
}