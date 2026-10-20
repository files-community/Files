// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Server.Cloud
{
	/// <summary>
	/// Provides an utility for generic cloud detection.
	/// </summary>
	internal sealed class GenericCloudDetector : AbstractCloudDetector
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
