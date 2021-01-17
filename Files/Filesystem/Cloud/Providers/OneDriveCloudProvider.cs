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
    public class OneDriveCloudProvider : ICloudProviderDetector
    {
        public async Task<IEnumerable<CloudProvider>> DetectAsync()
        {
            try
            {
                var attempt = 0;
                while (!await AppServiceConnectionHelper.IsConnected())
                {
                    //For sanity, let's limit this to waiting for 60 seconds
                    //If we don't get a connection in that time there are bigger problems, so fail gracefully
                    if (attempt >= 120)
                    {
                        return Array.Empty<CloudProvider>();
                    }

                    await Task.Delay(500);
                    attempt++;
                }

                var response = await AppServiceConnectionHelper.Connection.SendMessageAsync(new ValueSet() { { "Arguments", "GetOneDriveAccounts" } });
                if (response.Status == AppServiceResponseStatus.Success)
                {
                    var results = new List<CloudProvider>();

                    foreach (var key in response.Message.Keys
                        .OrderByDescending(o => string.Equals(o, "OneDrive - Personal", StringComparison.OrdinalIgnoreCase))
                        .ThenBy(o => o))
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