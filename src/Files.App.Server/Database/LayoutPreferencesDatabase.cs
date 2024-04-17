// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Server.Data;
using LiteDB;
using Windows.Storage;

namespace Files.App.Server.Database
{
	public sealed class LayoutPreferencesDatabase
	{
		private const string LayoutPreferences = "layoutprefs";
		private readonly static LiteDatabase Database;
		private readonly static string LayoutSettingsDbPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "user_settings.db");

		static LayoutPreferencesDatabase()
		{
			Database = new(
				new ConnectionString(LayoutSettingsDbPath)
				{
					Connection = ConnectionType.Direct,
					Upgrade = true,
				});
		}

		public LayoutPreferencesItem? GetPreferences(string? filePath, ulong? frn)
		{
			return FindPreferences(filePath, frn)?.LayoutPreferencesManager;
		}

		public void SetPreferences(string filePath, ulong? frn, LayoutPreferencesItem? preferencesItem)
		{
			// Get a collection (or create, if doesn't exist)
			var col = Database.GetCollection<LayoutPreferences>(LayoutPreferences);

			var tmp = FindPreferences(filePath, frn);

			if (tmp is null)
			{
				if (preferencesItem is not null)
				{
					// Insert new tagged file (Id will be auto-incremented)
					var newPref = new LayoutPreferences()
					{
						FilePath = filePath,
						Frn = frn,
						LayoutPreferencesManager = preferencesItem
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
					tmp.LayoutPreferencesManager = preferencesItem;
					col.Update(tmp);
				}
				else
				{
					// Remove file tag
					col.Delete(tmp.Id);
				}
			}
		}

		public void ResetAll()
		{
			var col = Database.GetCollection<LayoutPreferences>(LayoutPreferences);

			col.DeleteAll();
		}

		public void ApplyToAll(LayoutPreferencesUpdateAction updateAction)
		{
			var col = Database.GetCollection<LayoutPreferences>(LayoutPreferences);

			var allDocs = col.FindAll();

			foreach (var doc in allDocs)
			{
				updateAction(doc);
			}

			col.Update(allDocs);
		}

		public void Import(string json)
		{
			var dataValues = JsonSerializer.DeserializeArray(json);

			var col = Database.GetCollection(LayoutPreferences);

			col.DeleteAll();
			col.InsertBulk(dataValues.Select(x => x.AsDocument));
		}

		public string Export()
		{
			return JsonSerializer.Serialize(new BsonArray(Database.GetCollection(LayoutPreferences).FindAll()));
		}

		private LayoutPreferences? FindPreferences(string? filePath, ulong? frn)
		{
			// Get a collection (or create, if doesn't exist)
			var col = Database.GetCollection<LayoutPreferences>(LayoutPreferences);

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
	}

	public delegate void LayoutPreferencesUpdateAction(LayoutPreferences preference);
}
