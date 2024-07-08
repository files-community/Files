// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.Data.Sqlite;
using System.IO;
using Windows.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace Files.App.Utils.Cloud
{
	/// <summary>
	/// Provides an utility for Google Drive Cloud detection.
	/// </summary>
	public sealed class GoogleDriveCloudDetector : AbstractCloudDetector
	{
		private static readonly ILogger _logger = Ioc.Default.GetRequiredService<ILogger<App>>();

        private const string _googleDriveRegKeyName = @"Software\Google\DriveFS";

        private const string _googleDriveRegValName = "PerAccountPreferences";

        private const string _googleDriveRegValPropName = "value";

        private const string _googleDriveRegValPropPropName = "mount_point_path";

		protected override async IAsyncEnumerable<ICloudProvider> GetProviders()
		{
			// Google Drive's sync database can be in a couple different locations. Go find it.
			string appDataPath = UserDataPaths.GetDefault().LocalAppData;

			await StorageFile.GetFileFromPathAsync(Path.Combine(appDataPath, @"Google\DriveFS\root_preference_sqlite.db")).AsTask()
				.AndThen(c => c.CopyAsync(ApplicationData.Current.TemporaryFolder, "google_drive.db", NameCollisionOption.ReplaceExisting).AsTask());

			// The wal file may not exist but that's ok
			await FilesystemTasks.Wrap(() => StorageFile.GetFileFromPathAsync(Path.Combine(appDataPath, @"Google\DriveFS\root_preference_sqlite.db-wal")).AsTask()
				.AndThen(c => c.CopyAsync(ApplicationData.Current.TemporaryFolder, "google_drive.db-wal", NameCollisionOption.ReplaceExisting).AsTask()));

			var syncDbPath = Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "google_drive.db");

			// Build the connection and sql command
			SQLitePCL.Batteries_V2.Init();
			using var database = new SqliteConnection($"Data Source='{syncDbPath}'");
			using var cmdRoot = new SqliteCommand("SELECT * FROM roots", database);
			using var cmdMedia = new SqliteCommand("SELECT * FROM media WHERE fs_type=10", database);

			// Open the connection and execute the command
			database.Open();

			var reader = cmdRoot.ExecuteReader(); // Google synced folders
			while (reader.Read())
			{
				// Extract the data from the reader
				string? path = reader["last_seen_absolute_path"]?.ToString();
				if (string.IsNullOrWhiteSpace(path))
				{
					continue;
				}

				// By default, the path will be prefixed with "\\?\" (unless another app has explicitly changed it).
				// \\?\ indicates to Win32 that the filename may be longer than MAX_PATH (see MSDN).
				// Parts of .NET (e.g. the File class) don't handle this very well, so remove this prefix.
				if (path.StartsWith(@"\\?\", StringComparison.Ordinal))
				{
					path = path.Substring(@"\\?\".Length);
				}

				var folder = await StorageFolder.GetFolderFromPathAsync(path);
				string title = reader["title"]?.ToString() ?? folder.Name;

				App.AppModel.GoogleDrivePath = path;

				yield return new CloudProvider(CloudProviders.GoogleDrive)
				{
					Name = $"Google Drive ({title})",
					SyncFolder = path,
				};
			}

			// Google virtual drive
			reader = cmdMedia.ExecuteReader();

			while (reader.Read())
			{
				string? path = reader["last_mount_point"]?.ToString();
				if (string.IsNullOrWhiteSpace(path))
					continue;

				var folder = await StorageFolder.GetFolderFromPathAsync(path);
				string title = reader["name"]?.ToString() ?? folder.Name;

				App.AppModel.GoogleDrivePath = path;

				StorageFile? iconFile = await GetGoogleDriveIconFileAsync();

				yield return new CloudProvider(CloudProviders.GoogleDrive)
				{
					Name = title,
					SyncFolder = path,
					IconData = iconFile is not null ? await iconFile.ToByteArrayAsync() : null,
				};
			}

			await foreach (var provider in GetGoogleDriveProvidersFromRegistryAsync())
			{
				yield return provider;
			}
		}

		private JsonDocument? GetGoogleDriveRegValJson()
        {
            // This will be null if the key name is not found.
            using var googleDriveRegKey = Registry.CurrentUser.OpenSubKey(_googleDriveRegKeyName);

            if (googleDriveRegKey is null)
            {
                _logger.LogWarning(
                    "Google Drive registry key for key name `"
                        + _googleDriveRegKeyName
                        + "' not found."
                );
                return null;
            }

            var googleDriveRegVal = googleDriveRegKey.GetValue(_googleDriveRegValName);

            if (googleDriveRegVal is null)
            {
                _logger.LogWarning(
                    "Google Drive registry value for value name `"
                        + _googleDriveRegValName
                        + "' not found."
                );
                return null;
            }

            JsonDocument? googleDriveRegValueJson = null;
            try
            {
                googleDriveRegValueJson = JsonDocument.Parse(googleDriveRegVal.ToString() ?? "");
            }
            catch (JsonException je)
            {
                _logger.LogWarning(
                    je,
                    "Google Drive registry value for value name `"
                        + _googleDriveRegValName
                        + "' could not be parsed as a JsonDocument."
                );
            }

            return googleDriveRegValueJson;
        }

		private async IAsyncEnumerable<ICloudProvider> GetGoogleDriveProvidersFromRegistryAsync()
		{
            var googleDriveRegValJson = GetGoogleDriveRegValJson();

            if (googleDriveRegValJson is null)
				yield break;

			var googleDriveRegValJsonProperty = googleDriveRegValJson
				.RootElement.EnumerateObject()
				.FirstOrDefault();

			// A default JsonProperty struct has an "Undefined" Value.ValueKind and throws an
			// error if you try to call EnumerateArray on its Value.
			if (googleDriveRegValJsonProperty.Value.ValueKind == JsonValueKind.Undefined)
			{
				_logger.LogWarning(
					"Root element of Google Drive registry value for value name `"
						+ _googleDriveRegValName
						+ "' was empty."
				);
				yield break;
			}

			foreach (var item in googleDriveRegValJsonProperty.Value.EnumerateArray())
			{
				if (!item.TryGetProperty(_googleDriveRegValPropName, out var googleDriveRegValProp))
					yield break;

				if (!googleDriveRegValProp.TryGetProperty(_googleDriveRegValPropPropName,
					    out var googleDriveRegValPropProp))
					yield break;

				var path = googleDriveRegValPropProp.GetString();
				if (path is null)
					yield break;

				if (!ValidatePath(ref path))
					yield break;

				App.AppModel.GoogleDrivePath = path;

				var iconFile = await GetGoogleDriveIconFileAsync();

				yield return new CloudProvider(CloudProviders.GoogleDrive)
				{
					Name = "Google Drive",
					SyncFolder = path,
					IconData = iconFile is not null ? await iconFile.ToByteArrayAsync() : null,
				};
			}
		}

		private async Task<StorageFile?> GetGoogleDriveIconFileAsync()
		{
			var programFilesEnvVar = Environment.GetEnvironmentVariable("ProgramFiles");

			if (programFilesEnvVar is null)
				return null;

			var iconPath = Path.Combine(programFilesEnvVar, "Google", "Drive File Stream", "drive_fs.ico");

			return await FilesystemTasks.Wrap(() => StorageFile.GetFileFromPathAsync(iconPath).AsTask());
		}

		private bool ValidatePath(ref string path)
		{
			// If Google Drive is mounted as a drive, `path' will just be the drive letter, and
			// therefore needs to be reformatted as a valid path.

			if (path.Length == 1)
			{
				DriveInfo temp;
				try
				{
					temp = new DriveInfo(path);
				}
				catch (ArgumentException e)
				{
					_logger.LogWarning(e, "Could not resolve drive letter `" + path + "' to a valid drive.");
					return false;
				}

				path = temp.RootDirectory.Name;
			}

			if (Directory.Exists(path))
				return true;

			_logger.LogWarning("Invalid Google Drive mount point path: " + path);
			return false;
		}
	}
}
