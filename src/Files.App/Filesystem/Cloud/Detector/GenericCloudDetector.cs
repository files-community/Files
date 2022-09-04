using Files.Shared.Cloud;
using Files.App.Helpers;
using System.Collections.Generic;
using System.Text.Json;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;

namespace Files.App.Filesystem.Cloud
{
    public class GenericCloudDetector : AbstractCloudDetector
    {
        protected override async IAsyncEnumerable<ICloudProvider> GetProviders()
        {
            var connection = await AppServiceConnectionHelper.Instance;
            if (connection is not null)
            {
                var (status, response) = await connection.SendMessageForResponseAsync(new ValueSet
                {
                    ["Arguments"] = "DetectCloudDrives",
                });

                if (status is AppServiceResponseStatus.Success && response.ContainsKey("Drives"))
                {
                    var providers = JsonSerializer.Deserialize<List<CloudProvider>>(response["Drives"].GetString());
                    if (providers is not null)
                    {
                        foreach (var provider in providers)
                        {
                            yield return provider;
                        }
                    }
                }
            }
        }
    }
}