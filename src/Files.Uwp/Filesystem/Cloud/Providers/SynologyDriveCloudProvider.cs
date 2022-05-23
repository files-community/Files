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
        private class SynologyDriveConnection
        {
            public string ConnectionType { get; set; }

            public string HostName { get; set; }
        }

        public async Task<IList<CloudProvider>> DetectAsync()
        {
            /* Synology Drive stores its information on some files, but we only need sys.sqlite, which is placed on %LocalAppData%\SynologyDrive\data\db
             * In this database we need "connection_table" and "session_table" tables:
             * connection_table has the ids of each connection in the field "id", and the type of connection in the field "conn_type" (1 for sync tasks and 2 for backups)
             * Also it has "host_name" field where it's placed the name of each server.
             * session_table has the next fields:
             * "conn_id", which has the id that we check on connection_table to see if it's a sync or backup task
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
                using (var cmd = new SqliteCommand("select * from connection_table", con))
                using (var cmd2 = new SqliteCommand("select * from session_table", con))
                {
                    // Open the connection and execute the command
                    con.Open();
                    var reader = cmd.ExecuteReader();
                    var connections = new Dictionary<string, SynologyDriveConnection>();

                    while (reader.Read())
                    {
                        var connection = new SynologyDriveConnection()
                        {
                            ConnectionType = reader["conn_type"]?.ToString(),
                            HostName = reader["host_name"]?.ToString()
                        };

                        connections.Add(reader["id"]?.ToString(), connection);
                    }

                    var reader2 = cmd2.ExecuteReader();
                    var results = new List<CloudProvider>();

                    while (reader2.Read())
                    {
                        // Extract the data from the reader
                        if (connections[reader2["conn_id"]?.ToString()].ConnectionType == "1")
                        {
                            string path = reader2["sync_folder"]?.ToString();
                            if (string.IsNullOrWhiteSpace(path))
                            {
                                return Array.Empty<CloudProvider>();
                            }

                            var folder = await StorageFolder.GetFolderFromPathAsync(path);
                            var synologyDriveCloud = new CloudProvider()
                            {
                                ID = CloudProviders.SynologyDrive,
                                SyncFolder = path,
                                Name = $"Synology Drive - {connections[reader2["conn_id"]?.ToString()].HostName} ({folder.Name})"
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