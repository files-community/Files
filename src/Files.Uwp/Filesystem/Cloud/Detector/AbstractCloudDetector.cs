using Files.Shared.Cloud;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Files.Uwp.Filesystem.Cloud
{
    public abstract class AbstractCloudDetector : ICloudDetector
    {
        public async Task<IEnumerable<ICloudProvider>> DetectCloudProvidersAsync()
        {
            var providers = new List<ICloudProvider>();
            try
            {
                await foreach (var provider in GetProviders())
                {
                    providers.Add(provider);
                }
                return providers;
            }
            catch
            {
                return providers;
            }
        }

        protected abstract IAsyncEnumerable<ICloudProvider> GetProviders();
    }
}