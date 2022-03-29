using System.Collections.Generic;
using System.Threading.Tasks;
using Files.Shared;

namespace Files.Filesystem.Cloud
{
	public interface ICloudProviderDetector
	{
		Task<IList<CloudProvider>> DetectAsync();
	}
}