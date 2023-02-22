using Files.Core.Cloud;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Files.App.Filesystem.Cloud
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