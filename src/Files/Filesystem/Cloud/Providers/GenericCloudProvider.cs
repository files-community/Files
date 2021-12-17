using Files.Common;
using Files.Enums;
using Files.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;

namespace Files.Filesystem.Cloud.Providers
{
    public class GenericCloudProvider : ICloudProviderDetector
    {
        public async Task<IList<CloudProvider>> DetectAsync()
        {
            try
            {
                var connection = await AppServiceConnectionHelper.Instance;
                if (connection != null)
                {
                    var (status, response) = await connection.SendMessageForResponseAsync(new ValueSet()
                    {
                        { "Arguments", "DetectCloudDrives" }
                    });
                    if (status == AppServiceResponseStatus.Success && response.ContainsKey("Drives"))
                    {
                        var results = JsonConvert.DeserializeObject<List<CloudProvider>>((string)response["Drives"]);
                        if (results != null)
                        {
                            return results;
                        }
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