using Files.App.Shell;
using Files.Core.Cloud;
using Files.Core.Extensions;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading.Tasks;

namespace Files.App.Helpers
{
	[SupportedOSPlatform("Windows10.0.10240")]
	public class CloudDrivesDetector
	{
		private readonly static string programFilesFolder = Environment.GetEnvironmentVariable("ProgramFiles");

		public static async Task<IEnumerable<ICloudProvider>> DetectCloudDrives()
		{
			var tasks = new Task<IEnumerable<ICloudProvider>>[]
			{
				SafetyExtensions.IgnoreExceptions(DetectOneDrive, App.Logger),
				SafetyExtensions.IgnoreExceptions(DetectSharepoint, App.Logger),
				SafetyExtensions.IgnoreExceptions(DetectGenericCloudDrive, App.Logger),
				SafetyExtensions.IgnoreExceptions(DetectYandexDisk, App.Logger),
				SafetyExtensions.IgnoreExceptions(DetectpCloudDrive, App.Logger),
				SafetyExtensions.IgnoreExceptions(DetectNutstoreDrive, App.Logger),
				SafetyExtensions.IgnoreExceptions(DetectSeadriveDrive, App.Logger),
				SafetyExtensions.IgnoreExceptions(DetectAutodeskDrive, App.Logger),
			};

			await Task.WhenAll(tasks);

			return tasks
				.Where(o => o.Result is not null)
				.SelectMany(o => o.Result)
				.OrderBy(o => o.ID.ToString())
				.ThenBy(o => o.Name)
				.Distinct();
		}

		private static Task<IEnumerable<ICloudProvider>> DetectYandexDisk()
		{
			var results = new List<ICloudProvider>();
			using var yandexKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Yandex\Yandex.Disk.2");

			var syncedFolder = (string)yandexKey?.GetValue("RootFolder");
			if (syncedFolder is not null)
			{
				results.Add(new CloudProvider(CloudProviders.Yandex)
				{
					Name = $"Yandex Disk",
					SyncFolder = syncedFolder,
				});
			}

			return Task.FromResult<IEnumerable<ICloudProvider>>(results);
		}

		private static Task<IEnumerable<ICloudProvider>> DetectGenericCloudDrive()
		{
			var results = new List<ICloudProvider>();
			using var clsidKey = Registry.ClassesRoot.OpenSubKey(@"CLSID");
			using var namespaceKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Desktop\NameSpace");

			foreach (var subKeyName in namespaceKey.GetSubKeyNames())
			{
				using var clsidSubKey = SafetyExtensions.IgnoreExceptions(() => clsidKey.OpenSubKey(subKeyName));
				if (clsidSubKey is not null && (int?)clsidSubKey.GetValue("System.IsPinnedToNameSpaceTree") is 1)
				{
					using var namespaceSubKey = namespaceKey.OpenSubKey(subKeyName);
					var driveType = (string)namespaceSubKey?.GetValue(string.Empty);
					if (driveType is null)
					{
						continue;
					}

					//Nextcloud specific
					var appName = (string)namespaceSubKey?.GetValue("ApplicationName");
					if (!string.IsNullOrEmpty(appName) && appName == "Nextcloud")
					{
						driveType = appName;
					}

					// iCloud specific
					if (driveType.StartsWith("iCloudDrive"))
						driveType = "iCloudDrive";
					if (driveType.StartsWith("iCloudPhotos"))
						driveType = "iCloudPhotos";

					using var bagKey = clsidSubKey.OpenSubKey(@"Instance\InitPropertyBag");
					var syncedFolder = (string)bagKey?.GetValue("TargetFolderPath");
					if (syncedFolder is null)
					{
						continue;
					}

					// Also works for OneDrive, Box, Dropbox
					CloudProviders? driveID = driveType switch
					{
						"MEGA" => CloudProviders.Mega,
						"Amazon Drive" => CloudProviders.AmazonDrive,
						"Nextcloud" => CloudProviders.Nextcloud,
						"Jottacloud" => CloudProviders.Jottacloud,
						"iCloudDrive" => CloudProviders.AppleCloudDrive,
						"iCloudPhotos" => CloudProviders.AppleCloudPhotos,
						"Creative Cloud Files" => CloudProviders.AdobeCreativeCloud,
						_ => null,
					};
					if (driveID is null)
					{
						continue;
					}

					string nextCloudValue = (string)namespaceSubKey?.GetValue(string.Empty);
					results.Add(new CloudProvider(driveID.Value)
					{
						Name = driveID switch
						{
							CloudProviders.Mega => $"MEGA ({Path.GetFileName(syncedFolder.TrimEnd('\\'))})",
							CloudProviders.AmazonDrive => $"Amazon Drive",
							CloudProviders.Nextcloud => !string.IsNullOrEmpty(nextCloudValue) ? nextCloudValue : "Nextcloud",
							CloudProviders.Jottacloud => $"Jottacloud",
							CloudProviders.AppleCloudDrive => $"iCloud Drive",
							CloudProviders.AppleCloudPhotos => $"iCloud Photos",
							CloudProviders.AdobeCreativeCloud => $"Creative Cloud Files",
							_ => null
						},
						SyncFolder = syncedFolder,
					});
				}
			}

			return Task.FromResult<IEnumerable<ICloudProvider>>(results);
		}

		private static Task<IEnumerable<ICloudProvider>> DetectOneDrive()
		{
			using var oneDriveAccountsKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\OneDrive\Accounts");
			if (oneDriveAccountsKey is null)
			{
				return Task.FromResult<IEnumerable<ICloudProvider>>(null);
			}

			var oneDriveAccounts = new List<ICloudProvider>();
			foreach (var account in oneDriveAccountsKey.GetSubKeyNames())
			{
				var accountKeyName = @$"{oneDriveAccountsKey.Name}\{account}";
				var displayName = (string)Registry.GetValue(accountKeyName, "DisplayName", null);
				var userFolder = (string)Registry.GetValue(accountKeyName, "UserFolder", null);
				var accountName = string.IsNullOrWhiteSpace(displayName) ? "OneDrive" : $"OneDrive - {displayName}";

				if (!string.IsNullOrWhiteSpace(userFolder) && !oneDriveAccounts.Any(x => x.Name == accountName))
				{
					oneDriveAccounts.Add(new CloudProvider(CloudProviders.OneDrive)
					{
						Name = accountName,
						SyncFolder = userFolder,
					});
				}
			}

			return Task.FromResult<IEnumerable<ICloudProvider>>(oneDriveAccounts);
		}

		private static Task<IEnumerable<ICloudProvider>> DetectSharepoint()
		{
			using var oneDriveAccountsKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\OneDrive\Accounts");
			if (oneDriveAccountsKey is null)
			{
				return Task.FromResult<IEnumerable<ICloudProvider>>(null);
			}

			var sharepointAccounts = new List<ICloudProvider>();
			foreach (var account in oneDriveAccountsKey.GetSubKeyNames())
			{
				var accountKeyName = @$"{oneDriveAccountsKey.Name}\{account}";
				var displayName = (string)Registry.GetValue(accountKeyName, "DisplayName", null);
				var userFolderToExcludeFromResults = (string)Registry.GetValue(accountKeyName, "UserFolder", null);
				var accountName = string.IsNullOrWhiteSpace(displayName) ? "SharePoint" : $"SharePoint - {displayName}";

				var sharePointSyncFolders = new List<string>();
				var mountPointKeyName = @$"SOFTWARE\Microsoft\OneDrive\Accounts\{account}\ScopeIdToMountPointPathCache";
				using (var mountPointsKey = Registry.CurrentUser.OpenSubKey(mountPointKeyName))
				{
					if (mountPointsKey is null)
					{
						continue;
					}

					var valueNames = mountPointsKey.GetValueNames();
					foreach (var valueName in valueNames)
					{
						var value = (string)Registry.GetValue(@$"HKEY_CURRENT_USER\{mountPointKeyName}", valueName, null);
						if (!string.Equals(value, userFolderToExcludeFromResults, StringComparison.OrdinalIgnoreCase))
						{
							sharePointSyncFolders.Add(value);
						}
					}
				}

				sharePointSyncFolders.Sort(StringComparer.Ordinal);
				foreach (var sharePointSyncFolder in sharePointSyncFolders)
				{
					var parentFolder = Directory.GetParent(sharePointSyncFolder)?.FullName ?? string.Empty;
					if (!sharepointAccounts.Any(acc =>
						string.Equals(acc.Name, accountName, StringComparison.OrdinalIgnoreCase)) && !string.IsNullOrWhiteSpace(parentFolder))
					{
						sharepointAccounts.Add(new CloudProvider(CloudProviders.OneDriveCommercial)
						{
							Name = accountName,
							SyncFolder = parentFolder,
						});
					}
				}
			}

			return Task.FromResult<IEnumerable<ICloudProvider>>(sharepointAccounts);
		}

		private static Task<IEnumerable<ICloudProvider>> DetectpCloudDrive()
		{
			var results = new List<ICloudProvider>();
			using var pCloudDriveKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\pCloud");

			var syncedFolder = (string)pCloudDriveKey?.GetValue("SyncDrive");
			if (syncedFolder is not null)
			{
				string iconPath = Path.Combine(programFilesFolder, "pCloud Drive", "pCloud.exe");
				var iconFile = Win32API.ExtractSelectedIconsFromDLL(iconPath, new List<int>() { 32512 }, 32).FirstOrDefault();

				results.Add(new CloudProvider(CloudProviders.pCloud)
				{
					Name = $"pCloud Drive",
					SyncFolder = syncedFolder,
					IconData = iconFile?.IconData
				});
			}

			return Task.FromResult<IEnumerable<ICloudProvider>>(results);
		}

		private static Task<IEnumerable<ICloudProvider>> DetectNutstoreDrive()
		{
			var results = new List<ICloudProvider>();
			using var NutstoreKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Nutstore");

			if (NutstoreKey is not null)
			{
				string iconPath = Path.Combine(programFilesFolder, "Nutstore", "Nutstore.exe");
				var iconFile = Win32API.ExtractSelectedIconsFromDLL(iconPath, new List<int>() { 101 }).FirstOrDefault();

				// get every folder under the Nutstore folder in %userprofile%
				var mainFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Nutstore");
				var nutstoreFolders = Directory.GetDirectories(mainFolder, "Nutstore", SearchOption.AllDirectories);
				foreach (var nutstoreFolder in nutstoreFolders)
				{
					var folderName = Path.GetFileName(nutstoreFolder);
					if (folderName is not null && folderName.StartsWith("Nutstore", StringComparison.OrdinalIgnoreCase))
					{
						results.Add(new CloudProvider(CloudProviders.Nutstore)
						{
							Name = $"Nutstore",
							SyncFolder = nutstoreFolder,
							IconData = iconFile?.IconData
						});
					}
				}
			}

			return Task.FromResult<IEnumerable<ICloudProvider>>(results);
		}

		private static Task<IEnumerable<ICloudProvider>> DetectSeadriveDrive()
		{
			var results = new List<ICloudProvider>();
			using var SeadriveKey = Registry.CurrentUser.OpenSubKey(@"Software\SeaDrive\Seafile Drive Client\Settings");

			var syncFolder = (string)SeadriveKey?.GetValue("seadriveRoot");
			if (SeadriveKey is not null)
			{
				string iconPath = Path.Combine(programFilesFolder, "SeaDrive", "bin", "seadrive.exe");
				var iconFile = Win32API.ExtractSelectedIconsFromDLL(iconPath, new List<int>() { 101 }).FirstOrDefault();

				results.Add(new CloudProvider(CloudProviders.Seadrive)
				{
					Name = $"Seadrive",
					SyncFolder = syncFolder,
					IconData = iconFile?.IconData
				});
			}

			return Task.FromResult<IEnumerable<ICloudProvider>>(results);
		}

		private static Task<IEnumerable<ICloudProvider>> DetectAutodeskDrive()
		{
			var results = new List<ICloudProvider>();
			using var AutodeskKey = Registry.LocalMachine.OpenSubKey(@"Software\Autodesk\Desktop Connector");

			if (AutodeskKey is not null)
			{
				string iconPath = Path.Combine(programFilesFolder, "Autodesk", "Desktop Connector", "DesktopConnector.Applications.Tray.exe");
				var iconFile = Win32API.ExtractSelectedIconsFromDLL(iconPath, new List<int>() { 32512 }).FirstOrDefault();
				var mainFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "DC");
				var autodeskFolders = Directory.GetDirectories(mainFolder, "", SearchOption.AllDirectories);

				foreach (var autodeskFolder in autodeskFolders)
				{
					var folderName = Path.GetFileName(autodeskFolder);
					if (folderName is not null)
						results.Add(new CloudProvider(CloudProviders.Autodesk)
						{
							Name = $"Autodesk - {Path.GetFileName(autodeskFolder)}",
							SyncFolder = autodeskFolder,
							IconData = iconFile?.IconData
						});
				}
			}

			return Task.FromResult<IEnumerable<ICloudProvider>>(results);
		}
	}
}
