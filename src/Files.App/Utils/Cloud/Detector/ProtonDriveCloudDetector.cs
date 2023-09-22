using Files.App.Utils.Storage.Collection;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using Windows.Storage;

namespace Files.App.Utils.Cloud.Detector
{
	using SubKeys = DisposableCollection<RegistryKey>;

	public class ProtonDriveCloudDetector : AbstractCloudDetector
	{
		private const string DisplayName = "DisplayName";
		private const string InstallLocation = "InstallLocation";
		private const string InstalledSoftwareKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall";

		private const string ProtonDrive = "Proton Drive";
		private const string ProtonDriveIconPath = @"Resources\Icons\Logo.png";
		private const string ProtonDriveMappingsPath = @"Proton\Proton Drive\Mappings.json";

		private static string? GetInstallLocation()
		{
			return GetInstallLocation(Registry.LocalMachine) ?? GetInstallLocation(Registry.CurrentUser);
		}

		private static string? GetInstallLocation(RegistryKey rootKey)
		{
			using RegistryKey? parentKey = rootKey.OpenSubKey(InstalledSoftwareKey);

			using SubKeys? subKeys =
				parentKey?
					.GetSubKeyNames()?
					.AsParallel()
					.Select(subKey => parentKey?.OpenSubKey(subKey))?
					.ToList()?
					.AsDisposableCollection();

			using RegistryKey? protonInstallation =
				subKeys?
					.AsParallel()
					.FirstOrDefault(key => key?.GetValue(DisplayName)?.Equals(ProtonDrive) == true);

			return protonInstallation?.GetValue(InstallLocation)?.ToString();
		}

		protected override async IAsyncEnumerable<ICloudProvider> GetProviders()
		{
			// installLocation will have a trailing backslash
			if (GetInstallLocation() is string installLocation)
			{
				// relevant paths
				string appData = UserDataPaths.GetDefault().LocalAppData;
				StorageFile file = await StorageFile.GetFileFromPathAsync($@"{appData}\{ProtonDriveMappingsPath}");
				StorageFile icon = await StorageFile.GetFileFromPathAsync($@"{installLocation}{ProtonDriveIconPath}");

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
}
