using Files.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Files.Filesystem.Cloud.Providers
{
    public class OneDriveCloudProvider : ICloudProviderDetector
    {
        public async Task DetectAsync(List<CloudProvider> cloudProviders)
        {
            try
            {
                await Task.Run(() =>
                {
                    var onedrivePersonal = Environment.GetEnvironmentVariable("OneDriveConsumer");
                    if (!string.IsNullOrEmpty(onedrivePersonal))
                    {
                        cloudProviders.Add(new CloudProvider()
                        {
                            ID = CloudProviders.OneDrive,
                            Name = "OneDrive",
                            SyncFolder = onedrivePersonal
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