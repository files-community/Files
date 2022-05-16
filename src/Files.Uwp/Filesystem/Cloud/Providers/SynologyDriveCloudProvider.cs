using Files.Shared;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace Files.Uwp.Filesystem.Cloud.Providers
{
    public class SynologyDriveCloudProvider : ICloudProviderDetector
    {
        public async Task<IList<CloudProvider>> DetectAsync()
        {
            /* Synology Drive stores its information on some files, but we only need sys.sqlite, which is placed on %LocalAppData%\SynologyDrive\data\db
             * In this database we just need "session_table" table, and the fields:
             * "conn_id", which has the value 1 for backups and value 2 for sync tasks
             * "remote_path", which has the server folder. Currently it's not needed, just adding in case in the future is needed.
             * "sync_folder", which has the local folder to sync.
            */
            try
            {
                string appDataPath = UserDataPaths.GetDefault().LocalAppData;
                string dbPath = @"SynologyDrive\data\db\sys.sqlite";
                var configFile = await StorageFile.GetFileFromPathAsync(Path.Combine(appDataPath, dbPath));
                await configFile.CopyAsync(ApplicationData.Current.TemporaryFolder, "synology_drive.db", NameCollisionOption.ReplaceExisting);
                var syncDbPath = Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "synology_drive.db");

                // Build the connection and sql command
                SQLitePCL.Batteries_V2.Init();
                using (var con = new SqliteConnection($"Data Source='{syncDbPath}'"))
                using (var cmd = new SqliteCommand("select * from session_table", con))
                {
                    // Open the connection and execute the command
                    con.Open();
                    var reader = cmd.ExecuteReader();
                    var results = new List<CloudProvider>();

                    while (reader.Read())
                    {
                        // Extract the data from the reader
                        var isSyncTask = reader["conn_id"]?.ToString() == "2";
                        if (isSyncTask)
                        {
                            string path = reader["sync_folder"]?.ToString();
                            if (string.IsNullOrWhiteSpace(path))
                            {
                                return Array.Empty<CloudProvider>();
                            }

                            var folder = await StorageFolder.GetFolderFromPathAsync(path);
                            var synologyDriveCloud = new CloudProvider()
                            {
                                ID = CloudProviders.SynologyDrive,
                                SyncFolder = path,
                                Name = $"Synology Drive ({folder.Name})"
                            };

                            results.Add(synologyDriveCloud);

                        }
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