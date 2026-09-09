// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Utils.Cloud
{
	public abstract class AbstractCloudDetector : ICloudDetector
	{
		public async Task<IEnumerable<ICloudProvider>> DetectCloudProvidersAsync()
		{
			var providers = new List<ICloudProvider>();

			try
			{
				await foreach (var provider in GetProviders())
					providers.Add(provider);

				return providers;
			}
			catch
			{
				return providers;
			}
		}

		// Leaf detectors are consumed as whole batches by the aggregate; reuse the error-handled path.
		public async IAsyncEnumerable<ICloudProvider> DetectCloudProvidersProgressiveAsync()
		{
			foreach (var provider in await DetectCloudProvidersAsync())
				yield return provider;
		}

		protected abstract IAsyncEnumerable<ICloudProvider> GetProviders();
	}
}