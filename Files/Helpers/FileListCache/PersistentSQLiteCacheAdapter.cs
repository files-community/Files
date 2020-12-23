using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace Files.Helpers.FileListCache
{
    class PersistentSQLiteCacheAdapter : IFileListCache, IDisposable
    {
        private readonly SqliteConnection connection;
        private bool disposedValue;

        public PersistentSQLiteCacheAdapter()
        {
            var localCacheFolder = ApplicationData.Current.LocalCacheFolder.Path;
            string dbPath = Path.Combine(localCacheFolder, "cache.db");

            bool schemaCreated = File.Exists(dbPath);

            SQLitePCL.Batteries_V2.Init();

            connection = new SqliteConnection($"Data Source='{dbPath}'");
            connection.Open();

            if (!schemaCreated)
            {
                // create db schema
                var createSql = @"CREATE TABLE ""FileListCache"" (
                    ""Id"" VARCHAR(5000) NOT NULL,
                    ""Timestamp"" INTEGER NOT NULL,
	                ""Entry"" TEXT NOT NULL,
	                PRIMARY KEY(""Id"")
                )";
                using var cmd = new SqliteCommand(createSql, connection);
                var result = cmd.ExecuteNonQuery();
            }
        }
        public async Task SaveFileListToCache(string path, CacheEntry cacheEntry)
        {
            try
            {
                using var cmd = new SqliteCommand("SELECT Id FROM FileListCache WHERE Id = @Id", connection);
                cmd.Parameters.Add("@Id", SqliteType.Text).Value = path;
                using var reader = await cmd.ExecuteReaderAsync();
                if (reader.HasRows)
                {
                    // need to update entry
                    using var insertCommand = new SqliteCommand("UPDATE FileListCache SET Timestamp = @Timestamp, Entry = @Entry WHERE Id = @Id", connection);
                    insertCommand.Parameters.Add("@Id", SqliteType.Text).Value = path;
                    insertCommand.Parameters.Add("@Timestamp", SqliteType.Integer).Value = GetTimestamp(DateTime.UtcNow);
                    insertCommand.Parameters.Add("@Entry", SqliteType.Text).Value = JsonConvert.SerializeObject(cacheEntry);
                    await insertCommand.ExecuteNonQueryAsync();
                }
                else
                {
                    // need to insert entry
                    using var insertCommand = new SqliteCommand("INSERT INTO FileListCache (Id, Timestamp, Entry) VALUES (@Id, @Timestamp, @Entry)", connection);
                    insertCommand.Parameters.Add("@Id", SqliteType.Text).Value = path;
                    insertCommand.Parameters.Add("@Timestamp", SqliteType.Integer).Value = GetTimestamp(DateTime.UtcNow);
                    insertCommand.Parameters.Add("@Entry", SqliteType.Text).Value = JsonConvert.SerializeObject(cacheEntry);
                    await insertCommand.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        public async Task<CacheEntry> ReadFileListFromCache(string path)
        {
            try
            {
                using var cmd = new SqliteCommand("SELECT Timestamp, Entry FROM FileListCache WHERE Id = @Id", connection);
                cmd.Parameters.Add("@Id", SqliteType.Text).Value = path;

                using var reader = await cmd.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                {
                    return null;
                }
                var timestamp = reader.GetInt64(0);
                var entryAsJson = reader.GetString(1);
                var entry = JsonConvert.DeserializeObject<CacheEntry>(entryAsJson);
                entry.CurrentFolder.ItemPropertiesInitialized = false;
                entry.FileList.ForEach((item) => item.ItemPropertiesInitialized = false);
                return entry;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                return null;
            }
        }

        private long GetTimestamp(DateTime dateTime)
        {
            return new DateTimeOffset(dateTime).ToUnixTimeSeconds();
        }

        public void Dispose()
        {
            if (!disposedValue)
            {
                connection.Dispose();
                disposedValue = true;
            }
        }

        // TODO : run cleanup routine once in a while (remove all entries that wasn't used in a long time - use timestamp value here)
    }
}
