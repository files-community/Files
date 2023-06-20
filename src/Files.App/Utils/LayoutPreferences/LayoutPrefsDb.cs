// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Shared.Extensions;
using LiteDB;
using System;
using System.Linq;
using System.Text;
using IO = System.IO;

namespace Files.App.Helpers.LayoutPreferences
{
	public class LayoutPrefsDb : IDisposable
	{
		private readonly LiteDatabase db;

		public LayoutPrefsDb(string connection, bool shared = false)
		{
			SafetyExtensions.IgnoreExceptions(() => CheckDbVersion(connection));
			db = new LiteDatabase(new ConnectionString(connection)
			{
				Mode = shared ? LiteDB.FileMode.Shared : LiteDB.FileMode.Exclusive
			}, new BsonMapper() { IncludeFields = true });
		}

		public void SetPreferences(string filePath, ulong? frn, LayoutPreferences? prefs)
		{
			// Get a collection (or create, if doesn't exist)
			var col = db.GetCollection<LayoutDbPrefs>("layoutprefs");

			var tmp = _FindPreferences(filePath, frn);
			if (tmp is null)
			{
				if (prefs is not null)
				{
					// Insert new tagged file (Id will be auto-incremented)
					var newPref = new LayoutDbPrefs()
					{
						FilePath = filePath,
						Frn = frn,
						Prefs = prefs
					};
					col.Insert(newPref);
					col.EnsureIndex(x => x.Frn);
					col.EnsureIndex(x => x.FilePath);
				}
			}
			else
			{
				if (prefs is not null)
				{
					// Update file tag
					tmp.Prefs = prefs;
					col.Update(tmp);
				}
				else
				{
					// Remove file tag
					col.Delete(tmp.Id);
				}
			}
		}

		public LayoutPreferences? GetPreferences(string? filePath = null, ulong? frn = null)
		{
			return _FindPreferences(filePath, frn)?.Prefs;
		}

		private LayoutDbPrefs? _FindPreferences(string? filePath = null, ulong? frn = null)
		{
			// Get a collection (or create, if doesn't exist)
			var col = db.GetCollection<LayoutDbPrefs>("layoutprefs");

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

		public void ResetAll(Func<LayoutDbPrefs, bool>? predicate = null)
		{
			var col = db.GetCollection<LayoutDbPrefs>("layoutprefs");
			if (predicate is null)
			{
				col.Delete(Query.All());
			}
			else
			{
				col.Delete(x => predicate(x));
			}
		}

		public void ApplyToAll(Action<LayoutDbPrefs> updateAction, Func<LayoutDbPrefs, bool>? predicate = null)
		{
			var col = db.GetCollection<LayoutDbPrefs>("layoutprefs");
			var allDocs = predicate is null ? col.FindAll() : col.Find(x => predicate(x));
			allDocs.ForEach(x => updateAction(x));
			col.Update(allDocs);
		}

		~LayoutPrefsDb()
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
			var col = db.GetCollection("layoutprefs");
			col.Delete(Query.All());
			col.InsertBulk(dataValues.Select(x => x.AsDocument));
		}

		public string Export()
		{
			return JsonSerializer.Serialize(new BsonArray(db.GetCollection("layoutprefs").FindAll()));
		}

		// https://github.com/mbdavid/LiteDB/blob/master/LiteDB/Engine/Engine/Upgrade.cs
		private void CheckDbVersion(string filename)
		{
			var buffer = new byte[8192 * 2];
			using (var stream = new IO.FileStream(filename, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.ReadWrite))
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
			IO.File.Delete(filename); // recreate DB with correct version
		}

		public class LayoutDbPrefs
		{
			[BsonId]
			public int Id { get; set; }
			public ulong? Frn { get; set; }
			public string FilePath { get; set; } = string.Empty;
			public LayoutPreferences Prefs { get; set; } = LayoutPreferences.DefaultLayoutPreferences;
		}
	}
}
