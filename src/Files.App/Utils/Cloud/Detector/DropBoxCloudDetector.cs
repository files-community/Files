// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using System.IO;
using Windows.Storage;

namespace Files.App.Utils.Cloud
{
	/// <summary>
	/// Provides an utility for Drop Box Cloud detection.
	/// </summary>
	public sealed class DropBoxCloudDetector : AbstractCloudDetector
	{
		protected override async IAsyncEnumerable<ICloudProvider> GetProviders()
		{
			string? infoJsonPath = null;

			// First, try website version
			string websiteJsonPath = Path.Combine(UserDataPaths.GetDefault().LocalAppData, @"Dropbox\info.json");
			if (File.Exists(websiteJsonPath))
			{
				infoJsonPath = websiteJsonPath;
				App.Logger.LogInformation("Dropbox: Found website version at {Path}", websiteJsonPath);
			}
			else
			{
				// Fallback: Check Store versions
				string packagesPath = Path.Combine(UserDataPaths.GetDefault().LocalAppData, "Packages");
				if (Directory.Exists(packagesPath))
				{
					var dropboxPackages = Directory.GetDirectories(packagesPath, "DropboxInc.Dropbox_*");

					string? newestInfoJsonPath = null;
					DateTime newestTimestamp = DateTime.MinValue;

					foreach (var packageDir in dropboxPackages)
					{
						string storeJsonPath = Path.Combine(packageDir, @"LocalCache\Local\Dropbox\info.json");
						if (File.Exists(storeJsonPath))
						{
							var lastWriteTime = File.GetLastWriteTime(storeJsonPath);
							if (lastWriteTime > newestTimestamp)
							{
								newestTimestamp = lastWriteTime;
								newestInfoJsonPath = storeJsonPath;
							}
						}
					}

					if (newestInfoJsonPath is not null)
					{
						infoJsonPath = newestInfoJsonPath;
						App.Logger.LogInformation("Dropbox: Found Store version at {Path} (last modified: {Timestamp})", newestInfoJsonPath, newestTimestamp);
					}
				}
			}

			if (infoJsonPath is null)
				yield break;

			// Parse info.json and yield providers
			await foreach (var provider in ParseInfoJson(infoJsonPath))
			{
				yield return provider;
			}
		}

		private async IAsyncEnumerable<ICloudProvider> ParseInfoJson(string jsonPath)
		{
			var configFile = await StorageFile.GetFileFromPathAsync(jsonPath);
			using var jsonDoc = JsonDocument.Parse(await FileIO.ReadTextAsync(configFile));
			var jsonElem = jsonDoc.RootElement;

			if (jsonElem.TryGetProperty("personal", out JsonElement inner))
			{
				string dropBoxPath = inner.GetProperty("path").GetString();

				yield return new CloudProvider(CloudProviders.DropBox)
				{
					Name = "Dropbox",
					SyncFolder = dropBoxPath,
				};
			}

			if (jsonElem.TryGetProperty("business", out JsonElement innerBusiness))
			{
				string dropBoxPath = innerBusiness.GetProperty("path").GetString();

				yield return new CloudProvider(CloudProviders.DropBox)
				{
					Name = "Dropbox Business",
					SyncFolder = dropBoxPath,
				};
			}
		}
	}
}
