// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.IO;
using Windows.Storage;

namespace Files.App.Utils.Cloud
{
	/// <summary>
	/// Provides a utility for Google Drive Cloud detection.
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
			// Detect from Google Drive's persisted config only; touching the live shell/DriveFS to validate paths can block for ~19s.

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
			await using var database = new SqliteConnection($"Data Source='{syncDbPath}'");
			await using var cmdRoot = new SqliteCommand("SELECT * FROM roots", database);
			await using var cmdMedia = new SqliteCommand("SELECT * FROM media WHERE fs_type=10", database);

			database.Open();

			var iconFile = await GetGoogleDriveIconFileAsync();
			var iconData = iconFile is not null ? await iconFile.ToByteArrayAsync() : null;

			// Synced folders (Mirror mode)
			var reader = cmdRoot.ExecuteReader();
			while (reader.Read())
			{
				string? path = reader["last_seen_absolute_path"]?.ToString();
				if (string.IsNullOrWhiteSpace(path))
					continue;

				// The path is prefixed with "\\?\" by default, which parts of .NET (e.g. the File class) don't handle; strip it.
				if (path.StartsWith(@"\\?\", StringComparison.Ordinal))
					path = path[@"\\?\".Length..];

				yield return new CloudProvider(CloudProviders.GoogleDrive)
				{
					Name = $"Google Drive ({reader["title"]?.ToString() ?? Path.GetFileName(path)})",
					SyncFolder = path,
				};
			}

			// Virtual drive mount points (File Stream)
			reader = cmdMedia.ExecuteReader();
			while (reader.Read())
			{
				string? mount = reader["last_mount_point"]?.ToString();
				if (string.IsNullOrWhiteSpace(mount))
					continue;

				var path = Path.Combine(mount, "My Drive");
				yield return new CloudProvider(CloudProviders.GoogleDrive)
				{
					Name = reader["name"]?.ToString() ?? Path.GetFileName(path),
					SyncFolder = path,
					IconData = iconData,
				};
			}

			// Fall back to the base mount path from the registry (deduped downstream by path)
			var registryBasePath = GetRegistryBasePath();
			if (!string.IsNullOrEmpty(registryBasePath))
			{
				yield return new CloudProvider(CloudProviders.GoogleDrive)
				{
					Name = "Google Drive",
					SyncFolder = Path.Combine(registryBasePath, "My Drive"),
					IconData = iconData,
				};
			}
		}

		private static JsonDocument? GetGoogleDriveRegValJson()
		{
			// This will be null if the key name is not found.
			using var googleDriveRegKey = Registry.CurrentUser.OpenSubKey(_googleDriveRegKeyName);

			if (googleDriveRegKey is null)
				return null;

			var googleDriveRegVal = googleDriveRegKey.GetValue(_googleDriveRegValName);

			if (googleDriveRegVal is null)
				return null;

			JsonDocument? googleDriveRegValueJson = null;
			try
			{
				googleDriveRegValueJson = JsonDocument.Parse(googleDriveRegVal.ToString() ?? "");
			}
			catch (JsonException je)
			{
				_logger.LogWarning(je, $"Google Drive registry value for value name '{_googleDriveRegValName}' could not be parsed as a JsonDocument.");
			}

			return googleDriveRegValueJson;
		}

		/// <summary>
		/// Get the base file system path for Google Drive from the Registry.
		/// </summary>
		/// <remarks>
		/// For advanced "Google Drive for desktop" settings reference, see:
		/// https://support.google.com/a/answer/7644837
		/// </remarks>
		public static string? GetRegistryBasePath()
		{
			var googleDriveRegValJson = GetGoogleDriveRegValJson();

			if (googleDriveRegValJson is null)
				return null;

			var googleDriveRegValJsonProperty = googleDriveRegValJson
				.RootElement.EnumerateObject()
				.FirstOrDefault();

			// A default "JsonProperty" struct has an undefined "Value.ValueKind" and throws an
			// error if you try to call "EnumerateArray" on its value.
			if (googleDriveRegValJsonProperty.Value.ValueKind == JsonValueKind.Undefined)
			{
				_logger.LogWarning($"Root element of Google Drive registry value for value name '{_googleDriveRegValName}' was empty.");
				return null;
			}

			var item = googleDriveRegValJsonProperty.Value.EnumerateArray().FirstOrDefault();
			if (item.ValueKind == JsonValueKind.Undefined)
			{
				_logger.LogWarning($"Array in the root element of Google Drive registry value for value name '{_googleDriveRegValName}' was empty.");
				return null;
			}

			if (!item.TryGetProperty(_googleDriveRegValPropName, out var googleDriveRegValProp))
			{
				_logger.LogWarning($"First element in the Google Drive Registry Root Array did not have property named {_googleDriveRegValPropName}");
				return null;
			}

			if (!googleDriveRegValProp.TryGetProperty(_googleDriveRegValPropPropName, out var googleDriveRegValPropProp))
			{
				_logger.LogWarning($"Value from {_googleDriveRegValPropName} did not have property named {_googleDriveRegValPropPropName}");
				return null;
			}

			var path = googleDriveRegValPropProp.GetString();
			if (path is not null)
				return ConvertDriveLetterToPath(path);

			_logger.LogWarning($"Could not get string from value from {_googleDriveRegValPropPropName}");
			return null;
		}

		// A bare drive letter ("G") stored in the registry must be reformatted as a rooted path ("G:\")
		private static string ConvertDriveLetterToPath(string path)
			=> path.Length == 1 ? $@"{path}:\" : path;

		private static async Task<StorageFile?> GetGoogleDriveIconFileAsync()
		{
			var programFilesEnvVar = Environment.GetEnvironmentVariable("ProgramFiles");

			if (programFilesEnvVar is null)
				return null;

			var iconPath = Path.Combine(programFilesEnvVar, "Google", "Drive File Stream", "drive_fs.ico");

			var iconFileResult = await FilesystemTasks.Wrap(() => StorageFile.GetFileFromPathAsync(iconPath).AsTask());
			return iconFileResult ? iconFileResult.Result : null;
		}
	}
}
