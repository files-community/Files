// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Shared.Cloud;

namespace Files.App.Filesystem.Cloud
{
	public class GenericCloudDetector : AbstractCloudDetector
	{
		protected override async IAsyncEnumerable<ICloudProvider> GetProviders()
		{
			foreach (var provider in await CloudDrivesDetector.DetectCloudDrives())
			{
				yield return provider;
			}
		}
	}
}