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
        public async Task<IList<CloudProvider>> DetectAsync()
        {
            try
            {
                var connection = await AppServiceConnectionHelper.Instance;
                if (connection != null)
                {
                    var (status, response) = await connection.SendMessageWithRetryAsync(new ValueSet()
                    {
                        { "Arguments", "GetSharePointSyncLocationsFromOneDrive" }
                    }, TimeSpan.FromSeconds(10));
                    if (status == AppServiceResponseStatus.Success && response.Message.ContainsKey("Count"))
                    {
                        var results = new List<CloudProvider>();
                        foreach (var key in response.Message.Keys
                            .Where(k => k != "Count")
                            .OrderBy(o => o))
                        {
                            results.Add(new CloudProvider()
                            {
                                ID = CloudProviders.OneDrive,
                                Name = key,
                                SyncFolder = (string)response.Message[key]
                            });
                        }

                        return results;
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