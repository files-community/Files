using Files.Shared.Cloud;
using Files.App.Helpers;
using System.Collections.Generic;

namespace Files.App.Filesystem.Cloud
{
    public class GenericCloudDetector : AbstractCloudDetector
    {
        protected override async IAsyncEnumerable<ICloudProvider> GetProviders()
        {
            foreach (var provider in await CloudDrivesDetector.DetectCloudDrives())
            {
                yield return provider;
            }
        }
    }
}