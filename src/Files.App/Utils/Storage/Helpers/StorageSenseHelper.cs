// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Windows.Foundation.Metadata;
using Windows.System;

namespace Files.App.Utils.Storage
{
	internal class StorageSenseHelper
	{
		public static async Task OpenStorageSenseAsync(string path)
		{
			if (!path.StartsWith("C:", StringComparison.OrdinalIgnoreCase)
				&& ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
			{
				LaunchHelper.LaunchSettings("page=SettingsPageStorageSenseStorageOverview&target=SystemSettings_StorageSense_VolumeListLink");
			}
			else
			{
				await Launcher.LaunchUriAsync(new Uri("ms-settings:storagesense"));
			}
		}
	}
}
