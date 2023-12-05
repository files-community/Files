// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using LiteDB;
using System.Text;

namespace Files.App.Helpers
{
	/// <summary>
	/// Represents manager for the database of layout preferences.
	/// </summary>
	public class LayoutPreferencesDatabaseManager : IDisposable
	{
		// Fields

		private readonly LiteDatabase _database;

		// Constructor

		public LayoutPreferencesDatabaseManager(string databasePath, bool shared = false)
		{
			SafetyExtensions.IgnoreExceptions(() => EnsureDatabaseVersion(databasePath));

			_database = new(
				new ConnectionString(databasePath)
				{
					Mode = shared
						? FileMode.Shared
						: FileMode.Exclusive
				},
				new()
				{
					IncludeFields = true
				});
		}

		// Methods

		public LayoutPreferencesItem? GetPreferences(string? path = null, ulong? frn = null)
		{
			return FindPreferences(path, frn)?.LayoutPreferencesItem;
		}

		public void SetPreferences(string path, ulong? frn, LayoutPreferencesItem? preferencesItem)
		{
			// Get a collection (or create, if doesn't exist)
			var col = _database.GetCollection<LayoutPreferencesDatabaseItem>("layoutprefs");

			var tmp = FindPreferences(path, frn);

			if (tmp is null)
			{
				if (preferencesItem is not null)
				{
					// Insert new tagged file (Id will be auto-incremented)
					var newPref = new LayoutPreferencesDatabaseItem()
					{
						FilePath = path,
						Frn = frn,
						LayoutPreferencesItem = preferencesItem
					};

					col.Insert(newPref);
					col.EnsureIndex(x => x.Frn);
					col.EnsureIndex(x => x.FilePath);
				}
			}
			else
			{
				if (preferencesItem is not null)
				{
					// Update file tag
					tmp.LayoutPreferencesItem = preferencesItem;
					col.Update(tmp);
				}
				else
				{
					// Remove file tag
					col.Delete(tmp.Id);
				}
			}
		}

		public void ResetPreferencesAll(Func<LayoutPreferencesDatabaseItem, bool>? predicate = null)
		{
			var col = _database.GetCollection<LayoutPreferencesDatabaseItem>("layoutprefs");

			if (predicate is null)
			{
				col.Delete(Query.All());
			}
			else
			{
				col.Delete(x => predicate(x));
			}
		}

		public void ApplyPreferencesToAll(Action<LayoutPreferencesDatabaseItem> updateAction, Func<LayoutPreferencesDatabaseItem, bool>? predicate = null)
		{
			var col = _database.GetCollection<LayoutPreferencesDatabaseItem>("layoutprefs");

			var allDocs = predicate is null ? col.FindAll() : col.Find(x => predicate(x));

			allDocs.ForEach(x => updateAction(x));
			col.Update(allDocs);
		}

		public void ImportPreferences(string json)
		{
			var dataValues = JsonSerializer.DeserializeArray(json);

			var col = _database.GetCollection("layoutprefs");

			col.Delete(Query.All());
			col.InsertBulk(dataValues.Select(x => x.AsDocument));
		}

		public string ExportPreferences()
		{
			return JsonSerializer.Serialize(new BsonArray(_database.GetCollection("layoutprefs").FindAll()));
		}

		private LayoutPreferencesDatabaseItem? FindPreferences(string? path = null, ulong? frn = null)
		{
			// Get a collection (or create, if doesn't exist)
			var col = _database.GetCollection<LayoutPreferencesDatabaseItem>("layoutprefs");

			if (path is not null)
			{
				var tmp = col.FindOne(x => x.FilePath == path);
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
					if (path is not null)
					{
						// Keep entry updated
						tmp.FilePath = path;
						col.Update(tmp);
					}

					return tmp;
				}
			}

			return null;
		}

		private void EnsureDatabaseVersion(string filename)
		{
			// NOTE:
			//  For more information, visit
			//  https://github.com/mbdavid/LiteDB/blob/master/LiteDB/Engine/Engine/Upgrade.cs

			var databaseFileHeaderString = "** This is a LiteDB file **";
			var correctVersion = 7;
			var buffer = new byte[8192 * 2];

			// Get database file
			using var stream =
				new SystemIO.FileStream(
					filename,
					SystemIO.FileMode.Open,
					SystemIO.FileAccess.Read,
					SystemIO.FileShare.ReadWrite);

			// Read first 16k
			stream.Read(buffer, 0, buffer.Length);

			// Check if v7 (plain or encrypted)
			if (Encoding.UTF8.GetString(buffer, 25, databaseFileHeaderString.Length) == databaseFileHeaderString &&
				buffer[52] == correctVersion)
			{
				// version 4.1.4
				return;
			}

			// Re-create database with correct version
			SystemIO.File.Delete(filename);
		}

		// De-constructor & Disposer

		~LayoutPreferencesDatabaseManager()
		{
			Dispose();
		}

		public void Dispose()
		{
			_database.Dispose();
		}
	}
}
