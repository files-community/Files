// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Files.Shared.Cloud
{
	public interface ICloudDetector
	{
		Task<IEnumerable<ICloudProvider>> DetectCloudProvidersAsync();
	}
}