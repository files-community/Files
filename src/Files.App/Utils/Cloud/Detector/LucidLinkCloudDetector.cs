// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.IO;
using Windows.Storage;

namespace Files.App.Utils.Cloud
{
	/// <summary>
	/// Provides an utility for LucidLink Cloud detection.
	/// </summary>
	public sealed class LucidLinkCloudDetector : AbstractCloudDetector
	{
		private readonly string iconPath = Path.Combine(Environment.GetEnvironmentVariable("ProgramFiles"), "Lucid", "resources", "Logo.ico");

		protected override async IAsyncEnumerable<ICloudProvider> GetProviders()
		{
			string jsonPath = Path.Combine(Constants.UserEnvironmentPaths.HomePath, ".lucid", "app.json");
			string volumePath = Path.Combine(Constants.UserEnvironmentPaths.SystemDrivePath, "Volumes");

			var configFile = await StorageFile.GetFileFromPathAsync(jsonPath);
			using var jsonFile = JsonDocument.Parse(await FileIO.ReadTextAsync(configFile));
			var jsonElem = jsonFile.RootElement;

			if (jsonElem.TryGetProperty("filespaces", out JsonElement filespaces))
			{
				foreach (JsonElement inner in filespaces.EnumerateArray())
				{
					string syncFolder = inner.GetProperty("filespaceName").GetString();

					string[] orgNameFilespaceName = syncFolder.Split(".");
					string path = Path.Combine($@"{Constants.UserEnvironmentPaths.SystemDrivePath}\Volumes", orgNameFilespaceName[1], orgNameFilespaceName[0]);
					string filespaceName = orgNameFilespaceName[0];

					StorageFile iconFile = await FilesystemTasks.Wrap(() => StorageFile.GetFileFromPathAsync(iconPath).AsTask());

					yield return new CloudProvider(CloudProviders.LucidLink)
					{
						Name = $"Lucid Link ({filespaceName})",
						SyncFolder = path,
						IconData = iconFile is not null ? await iconFile.ToByteArrayAsync() : null,
					};
				}
			}

			// Lucid Link v3 and above
			await foreach (var provider in GetLucidLinkV3Providers(volumePath))
			{
				yield return provider;
			}
		}

		private async IAsyncEnumerable<ICloudProvider> GetLucidLinkV3Providers(string volumePath)
		{
			if (Directory.Exists(volumePath))
			{
				foreach (string directory in Directory.GetDirectories(volumePath))
				{
					if (IsSymlink(directory))
					{
						string[] orgNameFilespaceName = directory.Split("\\");
						string path = Path.Combine($@"{Constants.UserEnvironmentPaths.SystemDrivePath}\Volumes", orgNameFilespaceName[orgNameFilespaceName.Length - 1], orgNameFilespaceName[orgNameFilespaceName.Length - 2]);
						string filespaceName = orgNameFilespaceName[orgNameFilespaceName.Length - 2];

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

		private static bool IsSymlink(string path)
		{
			var fileInfo = new FileInfo(path);
			return fileInfo.Attributes.HasFlag(System.IO.FileAttributes.ReparsePoint);
		}
	}
}
