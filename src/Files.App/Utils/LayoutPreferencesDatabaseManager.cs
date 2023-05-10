// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using LiteDB;
using System.Text;

namespace Files.App.Helpers.LayoutPreferences
{
	public class LayoutPreferencesDatabaseManager : IDisposable
	{
		private readonly LiteDatabase db;

		internal LayoutPreferencesDatabaseManager(string connection, bool shared = false)
		{
			SafetyExtensions.IgnoreExceptions(() => CheckDbVersion(connection));

			db = new LiteDatabase(
				new ConnectionString(connection)
				{
					Mode = shared ? FileMode.Shared : FileMode.Exclusive
				},
				new() { IncludeFields = true },
				null);
		}

		internal void SetPreferences(string filePath, ulong? frn, LayoutPreferencesModel? prefs)
		{
			// Get a collection, or create one if it doesn't exist
			var col = db.GetCollection<LayoutPreferencesDatabaseItem>("layoutprefs");

			var tmp = FindPreferences(filePath, frn);
			if (tmp is null)
			{
				if (prefs is not null)
				{
					// Insert new tagged file
					// NOTE: Id will be auto-incremented
					var newPref = new LayoutPreferencesDatabaseItem()
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

		internal LayoutPreferencesModel? GetPreferences(string? filePath = null, ulong? frn = null)
		{
			return FindPreferences(filePath, frn)?.Prefs;
		}

		internal void ResetAll(Func<LayoutPreferencesDatabaseItem, bool>? predicate = null)
		{
			var col = db.GetCollection<LayoutPreferencesDatabaseItem>("layoutprefs");

			if (predicate is null)
				col.Delete(Query.All());
			else
				col.Delete(x => predicate(x));
		}

		internal void ApplyToAll(Action<LayoutPreferencesDatabaseItem> updateAction, Func<LayoutPreferencesDatabaseItem, bool>? predicate = null)
		{
			var col = db.GetCollection<LayoutPreferencesDatabaseItem>("layoutprefs");

			var allDocs = predicate is null ? col.FindAll() : col.Find(x => predicate(x));
			allDocs.ForEach(x => updateAction(x));

			col.Update(allDocs);
		}

		internal void Import(string json)
		{
			var dataValues = JsonSerializer.DeserializeArray(json);

			var col = db.GetCollection("layoutprefs");
			col.Delete(Query.All());
			col.InsertBulk(dataValues.Select(x => x.AsDocument));
		}

		internal string Export()
		{
			return JsonSerializer.Serialize(new BsonArray(db.GetCollection("layoutprefs").FindAll()));
		}

		/// <summary>
		/// Check LiteDB version of the database
		/// </summary>
		/// <remarks>
		/// <a href="https://github.com/mbdavid/LiteDB/blob/master/LiteDB/Engine/Engine/Upgrade.cs"/>
		/// </remarks>
		/// <param name="filename"></param>
		private static void CheckDbVersion(string filename)
		{
			var buffer = new byte[8192 * 2];
			using var stream = new SystemIO.FileStream(filename, SystemIO.FileMode.Open, SystemIO.FileAccess.Read, SystemIO.FileShare.ReadWrite);

			// Read first 16k bytes
			stream.Read(buffer, 0, buffer.Length);

			// Check if v7 (plain or encrypted)
			if (Encoding.UTF8.GetString(buffer, 25, "** This is a LiteDB file **".Length) == "** This is a LiteDB file **" && buffer[52] == 7)
				return; // Version 4.1.4

			SystemIO.File.Delete(filename); // Re-create new one with correct version
		}

		private LayoutPreferencesDatabaseItem? FindPreferences(string? filePath = null, ulong? frn = null)
		{
			// Get a collection (or create, if doesn't exist)
			var col = db.GetCollection<LayoutPreferencesDatabaseItem>("layoutprefs");

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

		public void Dispose()
		{
			db.Dispose();
		}

		~LayoutPreferencesDatabaseManager()
		{
			Dispose();
		}
	}
}
