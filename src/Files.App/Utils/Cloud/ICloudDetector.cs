// Copyright (c) 2018-2025 Files Community
// Licensed under the MIT License.

namespace Files.App.Utils.Cloud
{
	public interface ICloudDetector
	{
		Task<IEnumerable<ICloudProvider>> DetectCloudProvidersAsync();
	}
}
