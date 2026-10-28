// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Data.Sqlite;
using System.IO;
using Windows.Storage;

namespace Files.App.Server.Cloud
{
	/// <summary>
	/// Provides an utility for Synology Drive Cloud detection.
	/// </summary>
	internal sealed class SynologyDriveCloudDetector : AbstractCloudDetector
	{
		protected override async IAsyncEnumerable<ICloudProvider> GetProviders()
		{
			// Synology Drive stores its information on some files, but we only need sys.sqlite, which is placed on % LocalAppData %\SynologyDrive\data\db
			// In this database we need "connection_table" and "session_table" tables:
			// connection_table has the ids of each connection in the field "id", and the type of connection in the field "conn_type" (1 for sync tasks and 2 for backups)
			// Also it has "host_name" field where it's placed the name of each server.
			// session_table has the next fields:
			// "conn_id", which has the id that we check on connection_table to see if it's a sync or backup task
			// "remote_path", which has the server folder.Currently it's not needed, just adding in case in the future is needed.
			// "sync_folder", which has the local folder to sync.

			string appDataPath = UserDataPaths.GetDefault().LocalAppData;
			var databasePath = Path.Combine(appDataPath, @"SynologyDrive\data\db\sys.sqlite");
			if (!File.Exists(databasePath))
				yield break;

			// Build the connection and SQL command
			await using var database = CreateReadOnlyConnection(databasePath);
			await using var cmdConnection = new SqliteCommand("SELECT * FROM connection_table", database);
			await using var cmdTable = new SqliteCommand("SELECT * FROM session_table", database);

			// Open the connection and execute the command
			database.Open();
			var connections = new Dictionary<string, (string? ConnectionType, string? HostName)>();

			using (var reader = cmdConnection.ExecuteReader())
			{
				while (reader.Read())
				{
					var connection =
					(
						ConnectionType: reader["conn_type"]?.ToString(),
						HostName: reader["host_name"]?.ToString()
					);

					connections.Add(reader["id"]?.ToString()!, connection);
				}
			}

			using var sessions = cmdTable.ExecuteReader();
			while (sessions.Read())
			{
				// Extract the data from the reader
				var connectionId = sessions["conn_id"]?.ToString()!;
				if (connections.TryGetValue(connectionId, out var connection) && connection.ConnectionType is "1")
				{
					string? path = sessions["sync_folder"]?.ToString();

					if (string.IsNullOrWhiteSpace(path))
						continue;

					yield return new CloudProvider(CloudProviders.SynologyDrive)
					{
						SyncFolder = path,
						Name = $"Synology Drive - {connection.HostName} ({Path.GetFileName(Path.TrimEndingDirectorySeparator(path))})",
					};
				}
			}
		}
	}
}
