using Files.Enums;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Files.Filesystem.Cloud.Providers
{
    public class AmazonDriveProvider : ICloudProviderDetector
    {
        public async Task DetectAsync(List<CloudProvider> cloudProviders)
        {
            try
            {
                await Task.Run(() =>
                {
                    using var key = Registry.ClassesRoot.OpenSubKey(@"CLSID\{9B57F475-CCB0-4C85-88A9-2AA9A6C0809A}\Instance\InitPropertyBag");
                    if (key != null)
                    {
                        var syncedFolder = (string)key.GetValue("TargetFolderPath");

                        cloudProviders.Add(new CloudProvider()
                        {
                            ID = CloudProviders.AmazonDrive,
                            Name = "Amazon Drive",
                            SyncFolder = syncedFolder
                        });
                    }
                });
            }
            catch
            {
                // Not detected
            }
        }
    }
}