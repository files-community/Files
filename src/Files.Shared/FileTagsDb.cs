using LiteDB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Common
{
	public class FileTagsDb : IDisposable
	{
		private readonly LiteDatabase db;
		private const string TaggedFiles = "taggedfiles";

		public FileTagsDb(string connection, bool shared = false)
		{
			db = new LiteDatabase(new ConnectionString(connection)
			{
				Connection = shared ? ConnectionType.Shared : ConnectionType.Direct,
				Upgrade = true
			});
			UpdateDb();
		}

		public void SetTags(string filePath, ulong? frn, string[]? tags)
		{
			// Get a collection (or create, if doesn't exist)
			var col = db.GetCollection<TaggedFile>(TaggedFiles);

			var tmp = _FindTag(filePath, frn);
			if (tmp is null)
			{
				if (tags is not null && tags.Any())
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
				if (tags is not null && tags.Any())
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

		private TaggedFile? _FindTag(string? filePath = null, ulong? frn = null)
		{
			// Get a collection (or create, if doesn't exist)
			var col = db.GetCollection<TaggedFile>(TaggedFiles);

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

		public void UpdateTag(string oldFilePath, ulong? frn = null, string? newFilePath = null)
		{
			// Get a collection (or create, if doesn't exist)
			var col = db.GetCollection<TaggedFile>(TaggedFiles);
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

		public void UpdateTag(ulong oldFrn, ulong? frn = null, string? newFilePath = null)
		{
			// Get a collection (or create, if doesn't exist)
			var col = db.GetCollection<TaggedFile>(TaggedFiles);
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

		public string[]? GetTags(string? filePath = null, ulong? frn = null)
		{
			return _FindTag(filePath, frn)?.Tags;
		}

		public IEnumerable<TaggedFile> GetAll()
		{
			var col = db.GetCollection<TaggedFile>(TaggedFiles);
			return col.FindAll();
		}

		public IEnumerable<TaggedFile> GetAllUnderPath(string folderPath)
		{
			var col = db.GetCollection<TaggedFile>(TaggedFiles);
			return col.Find(x => x.FilePath.StartsWith(folderPath, StringComparison.OrdinalIgnoreCase));
		}

		~FileTagsDb()
		{
			Dispose();
		}

		public void Dispose()
		{
			db.Dispose();
		}

		public void Import(string json)
		{
			var dataValues = JsonSerializer.DeserializeArray(json);
			var col = db.GetCollection(TaggedFiles);
			col.DeleteAll();
			col.InsertBulk(dataValues.Select(x => x.AsDocument));
		}

		public string Export()
		{
			return JsonSerializer.Serialize(new BsonArray(db.GetCollection(TaggedFiles).FindAll()));
		}

		private void UpdateDb()
		{
			if (db.UserVersion == 0)
			{
				var col = db.GetCollection(TaggedFiles);
				foreach (var doc in col.FindAll())
				{
					doc["Tags"] = new BsonValue(new[] { doc["Tag"].AsString });
					doc.Remove("Tags");
					col.Update(doc);
				}
				db.UserVersion = 1;
			}
		}

		public class TaggedFile
		{
			[BsonId] public int Id { get; set; }
			public ulong? Frn { get; set; }
			public string FilePath { get; set; } = string.Empty;
			public string[] Tags { get; set; } = Array.Empty<string>();
		}
	}
}