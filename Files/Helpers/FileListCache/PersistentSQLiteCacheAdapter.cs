using Microsoft.Data.Sqlite;
using Microsoft.Toolkit.Uwp.Helpers;
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
        private SqliteConnection connection;
        private bool disposedValue;

        public async Task SaveFileListToCache(string path, CacheEntry cacheEntry)
        {
            if(!await InitializeIfNeeded())
            {
                return;
            }
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
                    updateCommand.Parameters.Add("@Entry", SqliteType.Text).Value = JsonConvert.SerializeObject(cacheEntry);
                    await updateCommand.ExecuteNonQueryAsync();
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
                NLog.LogManager.GetCurrentClassLogger().Error(ex, ex.Message);
            }
        }

        public async Task<CacheEntry> ReadFileListFromCache(string path, CancellationToken cancellationToken)
        {
            if (!await InitializeIfNeeded())
            {
                return null;
            }
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
                var entry = JsonConvert.DeserializeObject<CacheEntry>(entryAsJson);
                entry.CurrentFolder.ItemPropertiesInitialized = false;
                entry.FileList.ForEach((item) => item.ItemPropertiesInitialized = false);
                return entry;
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(ex, ex.Message);
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
                    NLog.LogManager.GetCurrentClassLogger().Error(ex, ex.Message);
                }
            });
        }

        private async Task<bool> InitializeIfNeeded()
        {
            if (disposedValue) return false;
            if (connection != null) return true;

            string dbPath = null;
            try
            {
                bool schemaCreated = await ApplicationData.Current.LocalFolder.FileExistsAsync("cache.db");
                await ApplicationData.Current.LocalFolder.CreateFileAsync("cache.db");

                dbPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "cache.db");

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

                RunCleanupRoutine();
                return true;
            } catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(ex, $"Failed initializing database with path: {dbPath}");
                return false;
            }
        }
    }
}