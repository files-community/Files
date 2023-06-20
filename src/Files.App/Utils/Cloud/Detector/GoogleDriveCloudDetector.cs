// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Shared.Cloud;
using Microsoft.Data.Sqlite;
using System.IO;
using Windows.Storage;

namespace Files.App.Filesystem.Cloud
{
	/// <summary>
	/// Provides an utility for Google Drive Cloud detection.
	/// </summary>
	public class GoogleDriveCloudDetector : AbstractCloudDetector
	{
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
				string iconPath = Path.Combine(Environment.GetEnvironmentVariable("ProgramFiles"), "Google", "Drive File Stream", "drive_fs.ico");

				StorageFile iconFile = await FilesystemTasks.Wrap(() => StorageFile.GetFileFromPathAsync(iconPath).AsTask());

				yield return new CloudProvider(CloudProviders.GoogleDrive)
				{
					Name = title,
					SyncFolder = path,
					IconData = iconFile is not null ? await iconFile.ToByteArrayAsync() : null,
				};
			}
		}
	}
}
