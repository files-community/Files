using Files.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Files.Filesystem.Cloud.Providers
{
    public class OneDriveCommercialCloudProvider : ICloudProviderDetector
    {
        public async Task DetectAsync(List<CloudProvider> cloudProviders)
        {
            try
            {
                await Task.Run(() =>
                {
                    var onedriveCommercial = Environment.GetEnvironmentVariable("OneDriveCommercial");
                    if (!string.IsNullOrEmpty(onedriveCommercial))
                    {
                        cloudProviders.Add(new CloudProvider()
                        {
                            ID = CloudProviders.OneDriveCommercial,
                            Name = "OneDrive Commercial",
                            SyncFolder = onedriveCommercial
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