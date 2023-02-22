using System.Collections.Generic;
using System.Threading.Tasks;

namespace Files.Core.Cloud
{
	public interface ICloudDetector
	{
		Task<IEnumerable<ICloudProvider>> DetectCloudProvidersAsync();
	}
}