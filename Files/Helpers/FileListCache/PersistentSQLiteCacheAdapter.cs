using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace Files.Helpers.FileListCache
{
    internal class PersistentSQLiteCacheAdapter : IFileListCache, IDisposable
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
                var createSql = @"CREATE TABLE IF NOT EXISTS ""FileListCache"" (
                    ""Id"" VARCHAR(5000) NOT NULL,
                    ""Timestamp"" INTEGER NOT NULL,
	                ""Entry"" TEXT NOT NULL,
	                PRIMARY KEY(""Id"")
                )";
                using var cmd = new SqliteCommand(createSql, connection);
                var result = cmd.ExecuteNonQuery();
            }

            RunCleanupRoutine();
        }

        public async Task SaveFileListToCache(string path, CacheEntry cacheEntry)
        {
            const int maxCachedEntries = 128;
            try
            {
                if (cacheEntry == null)
                {
                    using var deleteCommand = new SqliteCommand("DELETE FROM FileListCache WHERE Id = @Id", connection);
                    deleteCommand.Parameters.Add("@Id", SqliteType.Text).Value = path;
                    await deleteCommand.ExecuteNonQueryAsync();
                    return;
                }

                if (cacheEntry.FileList.Count > maxCachedEntries)
                {
                    cacheEntry.FileList = cacheEntry.FileList.Take(maxCachedEntries).ToList();
                }

                using var cmd = new SqliteCommand("SELECT Id FROM FileListCache WHERE Id = @Id", connection);
                cmd.Parameters.Add("@Id", SqliteType.Text).Value = path;
                using var reader = await cmd.ExecuteReaderAsync();
                if (reader.HasRows)
                {
                    // need to update entry
                    using var updateCommand = new SqliteCommand("UPDATE FileListCache SET Timestamp = @Timestamp, Entry = @Entry WHERE Id = @Id", connection);
                    updateCommand.Parameters.Add("@Id", SqliteType.Text).Value = path;
                    updateCommand.Parameters.Add("@Timestamp", SqliteType.Integer).Value = GetTimestamp(DateTime.UtcNow);
                    var settings = new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.Auto
                    };
                    updateCommand.Parameters.Add("@Entry", SqliteType.Text).Value = JsonConvert.SerializeObject(cacheEntry, settings);
                    await updateCommand.ExecuteNonQueryAsync();
                }
                else
                {
                    // need to insert entry
                    using var insertCommand = new SqliteCommand("INSERT INTO FileListCache (Id, Timestamp, Entry) VALUES (@Id, @Timestamp, @Entry)", connection);
                    insertCommand.Parameters.Add("@Id", SqliteType.Text).Value = path;
                    insertCommand.Parameters.Add("@Timestamp", SqliteType.Integer).Value = GetTimestamp(DateTime.UtcNow);
                    var settings = new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.Auto
                    };
                    insertCommand.Parameters.Add("@Entry", SqliteType.Text).Value = JsonConvert.SerializeObject(cacheEntry, settings);
                    await insertCommand.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        public async Task<CacheEntry> ReadFileListFromCache(string path, CancellationToken cancellationToken)
        {
            try
            {
                using var cmd = new SqliteCommand("SELECT Timestamp, Entry FROM FileListCache WHERE Id = @Id", connection);
                cmd.Parameters.Add("@Id", SqliteType.Text).Value = path;

                using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                if (!await reader.ReadAsync())
                {
                    return null;
                }
                var timestamp = reader.GetInt64(0);
                var entryAsJson = reader.GetString(1);
                var settings = new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto
                };
                var entry = JsonConvert.DeserializeObject<CacheEntry>(entryAsJson, settings);
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

        private void RunCleanupRoutine()
        {
            Task.Run(async () =>
            {
                try
                {
                    // remove entries that are 1 month old (timestamp is updated every time the cache is set)
                    var limitTimestamp = GetTimestamp(DateTime.Now.AddMonths(-1));
                    using var cmd = new SqliteCommand("DELETE FROM FileListCache WHERE Timestamp < @Timestamp", connection);
                    cmd.Parameters.Add("@Timestamp", SqliteType.Integer).Value = limitTimestamp;

                    var count = await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                    Debug.WriteLine($"Removed {count} old entries from cache database");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                }
            });
        }
    }
}