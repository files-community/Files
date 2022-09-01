using System.Collections.Generic;
using System.Threading.Tasks;

namespace Files.Shared.Cloud
{
    public interface ICloudDetector
    {
        Task<IEnumerable<ICloudProvider>> DetectCloudProvidersAsync();
    }
}