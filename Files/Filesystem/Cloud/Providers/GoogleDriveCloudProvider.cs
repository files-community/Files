﻿using Files.Enums;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace Files.Filesystem.Cloud.Providers
{
    public class GoogleDriveCloudProvider : ICloudProviderDetector
    {
        public async Task<IEnumerable<CloudProvider>> DetectAsync()
        {
            try
            {
                // Google Drive's sync database can be in a couple different locations. Go find it.
                string appDataPath = UserDataPaths.GetDefault().LocalAppData;
                string dbPath = @"Google\Drive\user_default\sync_config.db";
                var configFile = await StorageFile.GetFileFromPathAsync(Path.Combine(appDataPath, dbPath));
                await configFile.CopyAsync(ApplicationData.Current.TemporaryFolder, "google_drive.db", NameCollisionOption.ReplaceExisting);
                var syncDbPath = Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "google_drive.db");

                // Build the connection and sql command
                SQLitePCL.Batteries_V2.Init();
                using (var con = new SqliteConnection($"Data Source='{syncDbPath}'"))
                using (var cmd = new SqliteCommand("select * from data where entry_key='root_config__0'", con)) //local_sync_root_path
                {
                    // Open the connection and execute the command
                    con.Open();
                    var reader = cmd.ExecuteReader();
                    var results = new List<CloudProvider>();

                    while (reader.Read())
                    {
                        // Extract the data from the reader
                        string path = reader["data_value"]?.ToString();
                        if (string.IsNullOrWhiteSpace(path))
                        {
                            return Array.Empty<CloudProvider>();
                        }

                        // By default, the path will be prefixed with "\\?\" (unless another app has explicitly changed it).
                        // \\?\ indicates to Win32 that the filename may be longer than MAX_PATH (see MSDN).
                        // Parts of .NET (e.g. the File class) don't handle this very well, so remove this prefix.
                        if (path.StartsWith(@"\\?\"))
                        {
                            path = path.Substring(@"\\?\".Length);
                        }

                        var folder = await StorageFolder.GetFolderFromPathAsync(path);
                        var googleCloud = new CloudProvider()
                        {
                            ID = CloudProviders.GoogleDrive,
                            SyncFolder = path
                        };

                        if (!folder.Name.Contains("Google"))
                        {
                            googleCloud.Name = $"Google Drive ({folder.Name})";
                        }
                        else
                        {
                            googleCloud.Name = "Google Drive";
                        }

                        results.Add(googleCloud);
                    }

                    return results;
                }
            }
            catch
            {
                // Not detected
                return Array.Empty<CloudProvider>();
            }
        }
    }
}