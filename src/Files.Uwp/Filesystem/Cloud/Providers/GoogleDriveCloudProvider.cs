using Files.Shared;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace Files.Uwp.Filesystem.Cloud.Providers
{
    public class GoogleDriveCloudProvider : ICloudProviderDetector
    {
        public async Task<IList<CloudProvider>> DetectAsync()
        {
            try
            {
                // Google Drive's sync database can be in a couple different locations. Go find it.
                string appDataPath = UserDataPaths.GetDefault().LocalAppData;
                string dbPath = @"Google\DriveFS\root_preference_sqlite.db";
                var configFile = await StorageFile.GetFileFromPathAsync(Path.Combine(appDataPath, dbPath));
                await configFile.CopyAsync(ApplicationData.Current.TemporaryFolder, "google_drive.db", NameCollisionOption.ReplaceExisting);
                var syncDbPath = Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "google_drive.db");

                // Build the connection and sql command
                SQLitePCL.Batteries_V2.Init();
                using (var con = new SqliteConnection($"Data Source='{syncDbPath}'"))
                using (var cmd = new SqliteCommand("select * from roots", con))
                using (var cmd2 = new SqliteCommand("select * from media where fs_type=10", con))
                {
                    // Open the connection and execute the command
                    con.Open();
                    var reader = cmd.ExecuteReader(); // Google synced folders
                    var results = new List<CloudProvider>();

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
                        var googleCloud = new CloudProvider()
                        {
                            ID = CloudProviders.GoogleDrive,
                            SyncFolder = path
                        };

                        string title = reader["title"]?.ToString() ?? folder.Name;
                        googleCloud.Name = $"Google Drive ({title})";

                        results.Add(googleCloud);
                    }

                    reader = cmd2.ExecuteReader(); // Google virtual drive
                    while (reader.Read())
                    {
                        string path = reader["last_mount_point"]?.ToString();
                        if (string.IsNullOrWhiteSpace(path))
                        {
                            continue;
                        }

                        var folder = await StorageFolder.GetFolderFromPathAsync(path);
                        var googleCloud = new CloudProvider()
                        {
                            ID = CloudProviders.GoogleDrive,
                            SyncFolder = path
                        };

                        string title = reader["name"]?.ToString() ?? folder.Name;
                        googleCloud.Name = title;

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