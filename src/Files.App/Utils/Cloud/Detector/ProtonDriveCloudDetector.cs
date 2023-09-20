using Newtonsoft.Json.Linq;
using Windows.Storage;

namespace Files.App.Utils.Cloud.Detector
{
	public class ProtonDriveCloudDetector : AbstractCloudDetector
	{
		private const string ProtonDrive = "Proton Drive";
		private const string ProgramFiles = "ProgramFiles";
		private const string ProtonDriveIconPath = @"Proton\Drive\Resources\Icons\Logo.png";
		private const string ProtonDriveMappingsPath = @"Proton\Proton Drive\Mappings.json";

		protected override async IAsyncEnumerable<ICloudProvider> GetProviders()
		{
			// relevant paths
			string appData = UserDataPaths.GetDefault().LocalAppData;
			string? programFiles = Environment.GetEnvironmentVariable(ProgramFiles);
			StorageFile file = await StorageFile.GetFileFromPathAsync($@"{appData}\{ProtonDriveMappingsPath}");
			StorageFile icon = await StorageFile.GetFileFromPathAsync($@"{programFiles}\{ProtonDriveIconPath}");

			// read in cloud provider information
			byte[] iconData = await icon.ToByteArrayAsync();
			string jsonString = await FileIO.ReadTextAsync(file);
			dynamic configuration = JObject.Parse(jsonString);

			foreach (dynamic mapping in configuration.Mappings)
			{
				yield return new CloudProvider(CloudProviders.ProtonDrive)
				{
					Name = ProtonDrive,
					IconData = iconData,
					SyncFolder = mapping.Local.RootFolderPath
				};
			}
		}
	}
}
