﻿using Files.Shared.Cloud;
using Files.Uwp.Extensions;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using Windows.Storage;

namespace Files.Uwp.Filesystem.Cloud
{
    public class GoogleDriveCloudDetector : AbstractCloudDetector
    {
        protected override async IAsyncEnumerable<ICloudProvider> GetProviders()
        {
            // Google Drive's sync database can be in a couple different locations. Go find it.
            string appDataPath = UserDataPaths.GetDefault().LocalAppData;
            var configFile = await StorageFile.GetFileFromPathAsync(Path.Combine(appDataPath, @"Google\DriveFS\root_preference_sqlite.db"));
            await configFile.CopyAsync(ApplicationData.Current.TemporaryFolder, "google_drive.db", NameCollisionOption.ReplaceExisting);
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
                string path = reader["last_seen_absolute_path"]?.ToString();
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

            reader = cmdMedia.ExecuteReader(); // Google virtual drive
            while (reader.Read())
            {
                string path = reader["last_mount_point"]?.ToString();
                if (string.IsNullOrWhiteSpace(path))
                {
                    continue;
                }

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