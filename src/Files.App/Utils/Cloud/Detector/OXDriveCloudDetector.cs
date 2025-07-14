// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Utils.Cloud;
using Microsoft.Win32;
using System.IO;
using System.Text.Json;
using Windows.Storage;
using static Vanara.PInvoke.Gdi32;

namespace Files.App.Utils.Cloud
{
	/// <summary>
	/// Provides an utility for OX Drive Cloud detection.
	/// </summary>
	public sealed class OXDriveCloudDetector : AbstractCloudDetector
	{
		protected override async IAsyncEnumerable<ICloudProvider> GetProviders()
		{
			var syncFolder = await GetOXDriveSyncFolder();
			if (!string.IsNullOrEmpty(syncFolder))
			{
				var iconFile = GetOXDriveIconFile();
				yield return new CloudProvider(CloudProviders.OXDrive)
				{
					Name = "OX Drive",
					SyncFolder = syncFolder,
					IconData = iconFile?.IconData
				};
			}
		}
		public static async Task<string?> GetOXDriveSyncFolder()
		{
			var jsonPath = Path.Combine(UserDataPaths.GetDefault().LocalAppData, "Open-Xchange", "OXDrive", "userConfig.json");
			if (!File.Exists(jsonPath))
				return null; 

			var configFile = await StorageFile.GetFileFromPathAsync(jsonPath);
			using var jsonDoc = JsonDocument.Parse(await FileIO.ReadTextAsync(configFile));
			var jsonElem = jsonDoc.RootElement;

			string? syncFolderPath = null;

			if (jsonElem.TryGetProperty("Accounts", out var accounts) && accounts.GetArrayLength() > 0)
			{
				var account = accounts[0];

				if (account.TryGetProperty("MainFolderPath", out var folderPathElem))
					syncFolderPath = folderPathElem.GetString();
			}

			return syncFolderPath;
		}

		private static IconFileInfo? GetOXDriveIconFile()
		{
			var installPath = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Open-Xchange\OXDrive", "InstallDir", null) as string;

			// Fallback to default known path if not found in the registry.
			if (string.IsNullOrEmpty(installPath))
			{
				var pfX86 = Environment.GetEnvironmentVariable("ProgramFiles(x86)");
				if (string.IsNullOrEmpty(pfX86))
					return null;

				installPath = Path.Combine(pfX86, "Open-Xchange", "OXDrive");
			}

			var oxDriveFilePath = Path.Combine(installPath, "OXDrive.exe");
			if (!File.Exists(oxDriveFilePath))
			{
				return null;
			}

			// Extract the icon from the OXDrive executable (though it is executable, it contains icons)
			var icons = Win32Helper.ExtractSelectedIconsFromDLL(oxDriveFilePath, new List<int> { 0 }, 32);
			return icons.FirstOrDefault();
		}
	}
}
