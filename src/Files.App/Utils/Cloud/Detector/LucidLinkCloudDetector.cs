// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Utils.Cloud;
using System.IO;
using System.Text.Json;
using Windows.Storage;

namespace Files.App.Utils.Cloud
{
	/// <summary>
	/// Provides an utility for LucidLink Cloud detection.
	/// </summary>
	public sealed class LucidLinkCloudDetector : AbstractCloudDetector
	{
		protected override async IAsyncEnumerable<ICloudProvider> GetProviders()
		{
			string jsonPath = Path.Combine(Environment.GetEnvironmentVariable("UserProfile"), ".lucid", "app.json");

			var configFile = await StorageFile.GetFileFromPathAsync(jsonPath);
			using var jsonFile = JsonDocument.Parse(await FileIO.ReadTextAsync(configFile));
			var jsonElem = jsonFile.RootElement;

			if (jsonElem.TryGetProperty("filespaces", out JsonElement filespaces))
			{
				foreach (JsonElement inner in filespaces.EnumerateArray())
				{
					string syncFolder = inner.GetProperty("filespaceName").GetString();

					string[] orgNameFilespaceName = syncFolder.Split(".");
					string path = Path.Combine($@"{Environment.GetEnvironmentVariable("SystemDrive")}\Volumes", orgNameFilespaceName[1], orgNameFilespaceName[0]);
					string filespaceName = orgNameFilespaceName[0];

					string iconPath = Path.Combine(Environment.GetEnvironmentVariable("ProgramFiles"), "Lucid", "resources", "Logo.ico");
					StorageFile iconFile = await FilesystemTasks.Wrap(() => StorageFile.GetFileFromPathAsync(iconPath).AsTask());

					yield return new CloudProvider(CloudProviders.LucidLink)
					{
						Name = $"Lucid Link ({filespaceName})",
						SyncFolder = path,
						IconData = iconFile is not null ? await iconFile.ToByteArrayAsync() : null,
					};
				}
			}
		}
	}
}