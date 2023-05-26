// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Shared.Cloud;
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
					providers.Add(provider);

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