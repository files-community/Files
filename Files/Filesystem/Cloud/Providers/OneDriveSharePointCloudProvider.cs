using Files.Enums;
using Files.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;

namespace Files.Filesystem.Cloud.Providers
{
    public class OneDriveSharePointCloudProvider : ICloudProviderDetector
    {
        public async Task DetectAsync(List<CloudProvider> cloudProviders)
        {
            try
            {
                using var connection = await AppServiceConnectionHelper.BuildConnection();
                var (status, response) = await connection.SendMessageWithRetryAsync(new ValueSet()
                {
                    { "Arguments", "GetSharePointSyncLocationsFromOneDrive" }
                }, TimeSpan.FromSeconds(10));
                if (status == AppServiceResponseStatus.Success)
                {
                    foreach (var key in response.Message.Keys
                        .OrderBy(o => o))
                    {
                        cloudProviders.Add(new CloudProvider()
                        {
                            ID = CloudProviders.OneDrive,
                            Name = key,
                            SyncFolder = (string)response.Message[key]
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