using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Common
{
    public class FileTagsDb : IDisposable
    {
        private SqliteConnection connection;
        private bool disposedValue;
        private string dbPath;

        public FileTagsDb(string dbPath)
        {
            this.dbPath = dbPath;
        }

        public void SetTag(string filePath, ulong? frn, string tag)
        {
            if (!InitializeIfNeeded())
            {
                return;
            }
            var tmp = FindTag(filePath, frn);
            if (tmp == null)
            {
                if (tag != null)
                {
                    // Insert new tagged file (Id will be auto-incremented)
                    using var insertCommand = new SqliteCommand("INSERT INTO TaggedFiles (Frn, FilePath, Tag) VALUES (@Frn, @FilePath, @Tag)", connection);
                    insertCommand.Parameters.Add("@Frn", SqliteType.Integer).Value = frn;
                    insertCommand.Parameters.Add("@FilePath", SqliteType.Text).Value = filePath;
                    insertCommand.Parameters.Add("@Tag", SqliteType.Text).Value = tag;
                    insertCommand.ExecuteNonQuery();
                }
            }
            else
            {
                if (tag != null)
                {
                    // Update file tag
                    using var updateCommand = new SqliteCommand("UPDATE TaggedFiles SET Tag = @Tag WHERE Id = @Id", connection);
                    updateCommand.Parameters.Add("@Id", SqliteType.Integer).Value = tmp.Id;
                    updateCommand.Parameters.Add("@Tag", SqliteType.Text).Value = tag;
                    updateCommand.ExecuteNonQuery();
                }
                else
                {
                    // Remove file tag
                    using var deleteCommand = new SqliteCommand("DELETE FROM TaggedFiles WHERE Id = @Id", connection);
                    deleteCommand.Parameters.Add("@Id", SqliteType.Integer).Value = tmp.Id;
                    deleteCommand.ExecuteNonQuery();
                }
            }
        }

        private TaggedFile FindTag(string filePath = null, ulong? frn = null)
        {
            if (!InitializeIfNeeded())
            {
                return null;
            }
            if (filePath != null)
            {
                using var cmd = new SqliteCommand("SELECT Id, Frn, Tag FROM TaggedFiles WHERE FilePath = @FilePath", connection);
                cmd.Parameters.Add("@FilePath", SqliteType.Text).Value = filePath;
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    var tmp = new TaggedFile() { Id = reader.GetInt64(0), Frn = (ulong)reader.GetInt64(1), FilePath = filePath, Tag = reader.GetString(2) };
                    if (frn != null)
                    {
                        // Keep entry updated
                        using var updateCommand = new SqliteCommand("UPDATE TaggedFiles SET Frn = @Frn WHERE Id = @Id", connection);
                        updateCommand.Parameters.Add("@Id", SqliteType.Integer).Value = tmp.Id;
                        updateCommand.Parameters.Add("@Frn", SqliteType.Integer).Value = frn;
                        updateCommand.ExecuteNonQuery();
                    }
                    return tmp;
                }
            }
            if (frn != null)
            {
                using var cmd = new SqliteCommand("SELECT Id, FilePath, Tag FROM TaggedFiles WHERE Frn = @Frn", connection);
                cmd.Parameters.Add("@Frn", SqliteType.Integer).Value = frn;
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    var tmp = new TaggedFile() { Id = reader.GetInt64(0), Frn = frn, FilePath = reader.GetString(1), Tag = reader.GetString(2) };
                    if (filePath != null)
                    {
                        // Keep entry updated
                        using var updateCommand = new SqliteCommand("UPDATE TaggedFiles SET FilePath = @FilePath WHERE Id = @Id", connection);
                        updateCommand.Parameters.Add("@Id", SqliteType.Integer).Value = tmp.Id;
                        updateCommand.Parameters.Add("@FilePath", SqliteType.Text).Value = filePath;
                        updateCommand.ExecuteNonQuery();
                    }
                    return tmp;
                }
            }
            return null;
        }

        public void UpdateTag(string oldFilePath, ulong? frn = null, string newFilePath = null)
        {
            if (!InitializeIfNeeded())
            {
                return;
            }
            using var cmd = new SqliteCommand("SELECT Id, Frn, Tag FROM TaggedFiles WHERE FilePath = @FilePath", connection);
            cmd.Parameters.Add("@FilePath", SqliteType.Text).Value = oldFilePath;
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                var tmp = new TaggedFile() { Id = reader.GetInt64(0), Frn = (ulong)reader.GetInt64(1), FilePath = oldFilePath, Tag = reader.GetString(2) };
                if (frn != null)
                {
                    using var updateCommand = new SqliteCommand("UPDATE TaggedFiles SET Frn = @Frn WHERE Id = @Id", connection);
                    updateCommand.Parameters.Add("@Id", SqliteType.Integer).Value = tmp.Id;
                    updateCommand.Parameters.Add("@Frn", SqliteType.Integer).Value = frn;
                    updateCommand.ExecuteNonQuery();
                }
                if (newFilePath != null)
                {
                    using var updateCommand = new SqliteCommand("UPDATE TaggedFiles SET FilePath = @FilePath WHERE Id = @Id", connection);
                    updateCommand.Parameters.Add("@Id", SqliteType.Integer).Value = tmp.Id;
                    updateCommand.Parameters.Add("@FilePath", SqliteType.Text).Value = newFilePath;
                    updateCommand.ExecuteNonQuery();
                }
            }
        }

        public void UpdateTag(ulong oldFrn, ulong? frn = null, string newFilePath = null)
        {
            if (!InitializeIfNeeded())
            {
                return;
            }
            using var cmd = new SqliteCommand("SELECT Id, FilePath, Tag FROM TaggedFiles WHERE Frn = @Frn", connection);
            cmd.Parameters.Add("@Frn", SqliteType.Integer).Value = oldFrn;
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                var tmp = new TaggedFile() { Id = reader.GetInt64(0), Frn = oldFrn, FilePath = reader.GetString(1), Tag = reader.GetString(2) };
                if (frn != null)
                {
                    using var updateCommand = new SqliteCommand("UPDATE TaggedFiles SET Frn = @Frn WHERE Id = @Id", connection);
                    updateCommand.Parameters.Add("@Id", SqliteType.Integer).Value = tmp.Id;
                    updateCommand.Parameters.Add("@Frn", SqliteType.Integer).Value = frn;
                    updateCommand.ExecuteNonQuery();
                }
                if (newFilePath != null)
                {
                    using var updateCommand = new SqliteCommand("UPDATE TaggedFiles SET FilePath = @FilePath WHERE Id = @Id", connection);
                    updateCommand.Parameters.Add("@Id", SqliteType.Integer).Value = tmp.Id;
                    updateCommand.Parameters.Add("@FilePath", SqliteType.Text).Value = newFilePath;
                    updateCommand.ExecuteNonQuery();
                }
            }
        }

        public string GetTag(string filePath = null, ulong? frn = null)
        {
            if (!InitializeIfNeeded())
            {
                return null;
            }
            return FindTag(filePath, frn)?.Tag;
        }

        public IEnumerable<TaggedFile> GetAll()
        {
            if (!InitializeIfNeeded())
            {
                yield break;
            }
            using var cmd = new SqliteCommand("SELECT Id, Frn, FilePath, Tag FROM TaggedFiles", connection);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var tmp = new TaggedFile() { Id = reader.GetInt64(0), Frn = (ulong)reader.GetInt64(1), FilePath = reader.GetString(2), Tag = reader.GetString(3) };
                yield return tmp;
            }
        }

        public IEnumerable<TaggedFile> GetAllUnderPath(string folderPath)
        {
            if (!InitializeIfNeeded())
            {
                yield break;
            }
            using var cmd = new SqliteCommand("SELECT Id, Frn, FilePath, Tag FROM TaggedFiles", connection);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var tmp = new TaggedFile() { Id = reader.GetInt64(0), Frn = (ulong)reader.GetInt64(1), FilePath = reader.GetString(2), Tag = reader.GetString(3) };
                if (tmp.FilePath.StartsWith(folderPath, StringComparison.OrdinalIgnoreCase))
                {
                    yield return tmp;
                }
            }
        }

        public void Dispose()
        {
            if (!disposedValue)
            {
                connection?.Dispose();
                disposedValue = true;
            }
        }

        private bool InitializeIfNeeded()
        {
            if (disposedValue) return false;
            if (connection != null) return true;

            try
            {
                SQLitePCL.Batteries_V2.Init();

                connection = new SqliteConnection($"Data Source='{dbPath}'");
                connection.Open();

                // create db schema
                var createTaggedFilesTable = @"CREATE TABLE IF NOT EXISTS ""TaggedFiles"" (
                    ""Id"" INTEGER,
	                ""Frn"" INTEGER,
                    ""FilePath"" VARCHAR(5000) NOT NULL,
                    ""Tag"" VARCHAR(40) NOT NULL,
	                PRIMARY KEY(""Id"")
                )";
                using var cmdTaggedFilesTable = new SqliteCommand(createTaggedFilesTable, connection);
                cmdTaggedFilesTable.ExecuteNonQuery();

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public class TaggedFile
        {
            public long Id { get; set; }
            public ulong? Frn { get; set; }
            public string FilePath { get; set; }
            public string Tag { get; set; }
        }
    }
}
