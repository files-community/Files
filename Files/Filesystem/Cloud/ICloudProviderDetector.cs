using System.Collections.Generic;
using System.Threading.Tasks;

namespace Files.Filesystem.Cloud
{
    public interface ICloudProviderDetector
    {
        Task<IList<CloudProvider>> DetectAsync();
    }
}