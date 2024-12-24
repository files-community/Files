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
			string volumePath = Path.Combine(Constants.UserEnvironmentPaths.SystemDrivePath, "Volumes");

			if (Directory.Exists(volumePath))
			{
				foreach (string directory in Directory.GetDirectories(volumePath))
				{
					await foreach (var provider in GetProvidersFromDirectory(directory))
					{
						yield return provider;
					}
				}
			}
		}

		private async IAsyncEnumerable<ICloudProvider> GetProvidersFromDirectory(string directory)
		{
			foreach (string subDirectory in Directory.GetDirectories(directory))
			{
				if (IsSymlink(subDirectory))
				{
					string[] orgNameFilespaceName = subDirectory.Split("\\");
					string path = Path.Combine($@"{Constants.UserEnvironmentPaths.SystemDrivePath}\Volumes", orgNameFilespaceName[^2], orgNameFilespaceName[^1]);
					string filespaceName = orgNameFilespaceName[^1];

					StorageFile iconFile = await FilesystemTasks.Wrap(() => StorageFile.GetFileFromPathAsync(iconPath).AsTask());

					yield return new CloudProvider(CloudProviders.LucidLink)
					{
						Name = $"Lucid Link ({filespaceName})",
						SyncFolder = path,
						IconData = iconFile is not null ? await iconFile.ToByteArrayAsync() : null,
					};
				}
				else
				{
					await foreach (var provider in GetProvidersFromDirectory(subDirectory))
					{
						yield return provider;
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
