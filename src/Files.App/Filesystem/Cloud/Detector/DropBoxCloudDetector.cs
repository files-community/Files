// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Shared.Cloud;
using System.IO;
using System.Text.Json;
using Windows.Storage;

namespace Files.App.Filesystem.Cloud
{
	public class DropBoxCloudDetector : AbstractCloudDetector
	{
		protected override async IAsyncEnumerable<ICloudProvider> GetProviders()
		{
			string jsonPath = Path.Combine(UserDataPaths.GetDefault().LocalAppData, @"Dropbox\info.json");
			var configFile = await StorageFile.GetFileFromPathAsync(jsonPath);
			using var jsonDoc = JsonDocument.Parse(await FileIO.ReadTextAsync(configFile));
			var jsonElem = jsonDoc.RootElement;

			if (jsonElem.TryGetProperty("personal", out JsonElement inner))
			{
				string dropboxPath = inner.GetProperty("path").GetString();

				yield return new CloudProvider(CloudProviders.DropBox)
				{
					Name = "Dropbox",
					SyncFolder = dropboxPath,
				};
			}

			if (jsonElem.TryGetProperty("business", out JsonElement innerBusiness))
			{
				string dropboxPath = innerBusiness.GetProperty("path").GetString();

				yield return new CloudProvider(CloudProviders.DropBox)
				{
					Name = "Dropbox Business",
					SyncFolder = dropboxPath,
				};
			}
		}
	}
}