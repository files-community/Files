// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Server.Data;
using Files.Shared.Extensions;
using LiteDB;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Foundation.Metadata;
using Windows.Storage;

namespace Files.App.Server.Database
{
	public sealed class FileTagsDatabase
	{
		private static LiteDatabase _database = default!;
		private static readonly object _lockObject = new();

		private const string TaggedFiles = "taggedfiles";

		public static string FileTagsDbPath 
			=> Path.Combine(ApplicationData.Current.LocalFolder.Path, "filetags.db");

		public FileTagsDatabase()
		{
			lock (_lockObject)
			{
				if (_database is null)
				{
					SafetyExtensions.IgnoreExceptions(() => CheckDbVersion(FileTagsDbPath));

					_database = new LiteDatabase(new ConnectionString(FileTagsDbPath)
					{
						Connection = ConnectionType.Direct,
						Upgrade = true
					});

					UpdateDb();
				}
			}
		}

		public void SetTags(string filePath, ulong? frn, [ReadOnlyArray] string[] tags)
		{
			// Get a collection (or create, if doesn't exist)
			var col = _database.GetCollection<TaggedFile>(TaggedFiles);

			var tmp = FindTag(filePath, frn);
			if (tmp is null)
			{
				if (tags.Any())
				{
					// Insert new tagged file (Id will be auto-incremented)
					var newTag = new TaggedFile()
					{
						FilePath = filePath,
						Frn = frn,
						Tags = tags
					};
					col.Insert(newTag);
					col.EnsureIndex(x => x.Frn);
					col.EnsureIndex(x => x.FilePath);
				}
			}
			else
			{
				if (tags.Any())
				{
					// Update file tag
					tmp.Tags = tags;
					col.Update(tmp);
				}
				else
				{
					// Remove file tag
					col.Delete(tmp.Id);
				}
			}
		}

		private TaggedFile? FindTag(string? filePath, ulong? frn)
		{
			// Get a collection (or create, if doesn't exist)
			var col = _database.GetCollection<TaggedFile>(TaggedFiles);

			if (filePath is not null)
			{
				var tmp = col.FindOne(x => x.FilePath == filePath);
				if (tmp is not null)
				{
					if (frn is not null)
					{
						// Keep entry updated
						tmp.Frn = frn;
						col.Update(tmp);
					}

					return tmp;
				}
			}

			if (frn is not null)
			{
				var tmp = col.FindOne(x => x.Frn == frn);
				if (tmp is not null)
				{
					if (filePath is not null)
					{
						// Keep entry updated
						tmp.FilePath = filePath;
						col.Update(tmp);
					}

					return tmp;
				}
			}

			return null;
		}

		[DefaultOverload]
		public void UpdateTag(string oldFilePath, ulong? frn, string? newFilePath)
		{
			// Get a collection (or create, if doesn't exist)
			var col = _database.GetCollection<TaggedFile>(TaggedFiles);
			var tmp = col.FindOne(x => x.FilePath == oldFilePath);
			if (tmp is not null)
			{
				if (frn is not null)
				{
					tmp.Frn = frn;
					col.Update(tmp);
				}

				if (newFilePath is not null)
				{
					tmp.FilePath = newFilePath;
					col.Update(tmp);
				}
			}
		}

		[Overload("UpdateTagByFrn")]
		public void UpdateTag(ulong oldFrn, ulong? frn, string? newFilePath)
		{
			// Get a collection (or create, if doesn't exist)
			var col = _database.GetCollection<TaggedFile>(TaggedFiles);
			var tmp = col.FindOne(x => x.Frn == oldFrn);
			if (tmp is not null)
			{
				if (frn is not null)
				{
					tmp.Frn = frn;
					col.Update(tmp);
				}

				if (newFilePath is not null)
				{
					tmp.FilePath = newFilePath;
					col.Update(tmp);
				}
			}
		}

		public string[]? GetTags(string? filePath, ulong? frn)
		{
			return FindTag(filePath, frn)?.Tags;
		}

		public IEnumerable<TaggedFile> GetAll()
		{
			var col = _database.GetCollection<TaggedFile>(TaggedFiles);
			return col.FindAll();
		}

		public IEnumerable<TaggedFile> GetAllUnderPath(string folderPath)
		{
			var col = _database.GetCollection<TaggedFile>(TaggedFiles);
			if (string.IsNullOrEmpty(folderPath))
				return col.FindAll();
			return col.Find(x => x.FilePath.StartsWith(folderPath, StringComparison.OrdinalIgnoreCase));
		}

		public void Import(string json)
		{
			var dataValues = JsonSerializer.DeserializeArray(json);
			var col = _database.GetCollection(TaggedFiles);
			col.DeleteAll();
			col.InsertBulk(dataValues.Select(x => x.AsDocument));
		}

		public string Export()
		{
			return JsonSerializer.Serialize(new BsonArray(_database.GetCollection(TaggedFiles).FindAll()));
		}

		private void UpdateDb()
		{
			if (_database.UserVersion == 0)
			{
				var col = _database.GetCollection(TaggedFiles);
				foreach (var doc in col.FindAll())
				{
					doc["Tags"] = new BsonValue(new[] { doc["Tag"].AsString });
					doc.Remove("Tags");
					col.Update(doc);
				}
				_database.UserVersion = 1;
			}
		}

		// https://github.com/mbdavid/LiteDB/blob/master/LiteDB/Engine/Engine/Upgrade.cs
		private void CheckDbVersion(string filename)
		{
			var buffer = new byte[8192 * 2];
			using (var stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
			{
				// read first 16k
				stream.Read(buffer, 0, buffer.Length);

				// checks if v7 (plain or encrypted)
				if (Encoding.UTF8.GetString(buffer, 25, "** This is a LiteDB file **".Length) == "** This is a LiteDB file **" &&
					buffer[52] == 7)
				{
					return; // version 4.1.4
				}
			}
			File.Delete(filename); // recreate DB with correct version
		}
	}
}
