using System.IO;
using Windows.Storage;

namespace Files.App.Utils.Cloud
{
	/// <summary>
	/// Provides a utility for Sync Cloud detection.
	/// </summary>
	public sealed class SyncCloudDetector : AbstractCloudDetector
	{
		protected override async IAsyncEnumerable<ICloudProvider> GetProviders()
		{
			string syncFolderPath = Path.Combine(Constants.UserEnvironmentPaths.HomePath, "Sync");

			if (Directory.Exists(syncFolderPath))
			{
				foreach (string directory in Directory.GetDirectories(syncFolderPath))
				{
					var folder = await StorageFolder.GetFolderFromPathAsync(directory);

					yield return new CloudProvider(CloudProviders.Sync)
					{
						Name = $"Sync - {folder.Name}",
						SyncFolder = directory,
						// IconData = (needs icon)
					};
				}
			}
		}
	}
}
