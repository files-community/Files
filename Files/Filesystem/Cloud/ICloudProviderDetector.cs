using System.Collections.Generic;
using System.Threading.Tasks;

namespace Files.Filesystem.Cloud
{
    public interface ICloudProviderDetector
    {
        public Task DetectAsync(List<CloudProvider> cloudProviders);
    }
}
