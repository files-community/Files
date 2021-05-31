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
        private SqliteConnection connection;
        private bool disposedValue;

        public async Task SaveFileDisplayNameToCache(string path, string displayName)
        {
            if (!await InitializeIfNeeded())
            {
                return;
            }
            try
            {
                if (displayName == null)
                {
                    using var deleteCommand = new SqliteCommand("DELETE FROM FileDisplayNameCache WHERE Id = @Id", connection);
                    deleteCommand.Parameters.Add("@Id", SqliteType.Text).Value = path;
                    await deleteCommand.ExecuteNonQueryAsync();
                    return;
                }

                using var cmd = new SqliteCommand("SELECT Id FROM FileDisplayNameCache WHERE Id = @Id", connection);
                cmd.Parameters.Add("@Id", SqliteType.Text).Value = path;
                using var reader = await cmd.ExecuteReaderAsync();
                if (reader.HasRows)
                {
                    // need to update entry
                    using var updateCommand = new SqliteCommand("UPDATE FileDisplayNameCache SET DisplayName = @DisplayName WHERE Id = @Id", connection);
                    updateCommand.Parameters.Add("@Id", SqliteType.Text).Value = path;
                    updateCommand.Parameters.Add("@DisplayName", SqliteType.Text).Value = displayName;
                    await updateCommand.ExecuteNonQueryAsync();
                }
                else
                {
                    // need to insert entry
                    using var insertCommand = new SqliteCommand("INSERT INTO FileDisplayNameCache (Id, DisplayName) VALUES (@Id, @DisplayName)", connection);
                    insertCommand.Parameters.Add("@Id", SqliteType.Text).Value = path;
                    insertCommand.Parameters.Add("@DisplayName", SqliteType.Text).Value = displayName;
                    await insertCommand.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        public async Task<string> ReadFileDisplayNameFromCache(string path, CancellationToken cancellationToken)
        {
            if (!await InitializeIfNeeded())
            {
                return null;
            }
            try
            {
                using var cmd = new SqliteCommand("SELECT DisplayName FROM FileDisplayNameCache WHERE Id = @Id", connection);
                cmd.Parameters.Add("@Id", SqliteType.Text).Value = path;

                using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                if (!await reader.ReadAsync())
                {
                    return null;
                }
                return reader.GetString(0);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                return null;
            }
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
        }

        private async Task<bool> InitializeIfNeeded()
        {
            if (disposedValue) return false;
            if (connection != null) return true;

            string dbPath = null;
            try
            {
                await ApplicationData.Current.LocalFolder.CreateFileAsync("cache.db", CreationCollisionOption.OpenIfExists);

                dbPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "cache.db");

                SQLitePCL.Batteries_V2.Init();

                connection = new SqliteConnection($"Data Source='{dbPath}'");
                connection.Open();

                // create db schema
                var createFileDisplayNameCacheTable = @"CREATE TABLE IF NOT EXISTS ""FileDisplayNameCache"" (
                    ""Id"" VARCHAR(5000) NOT NULL,
	                ""DisplayName"" TEXT NOT NULL,
	                PRIMARY KEY(""Id"")
                )";
                using var cmdFileDisplayNameCacheTable = new SqliteCommand(createFileDisplayNameCacheTable, connection);
                cmdFileDisplayNameCacheTable.ExecuteNonQuery();

                RunCleanupRoutine();
                return true;
            }
            catch (Exception ex)
            {
                App.Logger.Warn(ex, $"Failed initializing database with path: {dbPath}");
                return false;
            }
        }
    }
}