using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Files.Filesystem.Cloud.Providers
{
    public class MegaCloudProvider : ICloudProviderDetector
    {
        public async Task<IList<CloudProvider>> DetectAsync()
        {
            // MEGA not supported anymore
            return await Task.FromResult(Array.Empty<CloudProvider>());
        }
    }
}