// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Diagnostics.CodeAnalysis;

namespace Files.App.Helpers
{
	/// <summary>
	/// Represents manager for the database of layout preferences.
	/// </summary>
	public class LayoutPreferencesDatabaseManager
	{
		// Fields
		private readonly Server.Database.LayoutPreferencesDatabase _database = new();

		[return: NotNullIfNotNull(nameof(entry))]
		private object? CopyValues(Type fromType, Type toType, object? entry)
		{
			if (entry is null)
			{
				return null;
			}

			var result = Activator.CreateInstance(toType)!;

			foreach (var prop in fromType.GetProperties())
			{
				var toProp = toType.GetProperty(prop.Name);

				if (toProp is null)
				{
					continue;
				}

				if (!prop.PropertyType.IsPrimitive && !prop.PropertyType.IsEnum)
				{
					toProp.SetValue(result, CopyValues(prop.PropertyType, toProp.PropertyType, prop.GetValue(entry)));
				}
				else
				{
					toProp.SetValue(result, prop.GetValue(entry));
				}
			}

			return result;
		}

		// Methods
		public LayoutPreferencesItem? GetPreferences(string? filePath = null, ulong? frn = null)
		{
			return (LayoutPreferencesItem?)CopyValues(typeof(Server.Data.LayoutPreferencesItem), typeof(LayoutPreferencesItem), _database.GetPreferences(filePath, frn));
		}

		public void SetPreferences(string filePath, ulong? frn, LayoutPreferencesItem? preferencesItem)
		{
			_database.SetPreferences(filePath, frn, (Server.Data.LayoutPreferencesItem?)CopyValues(typeof(LayoutPreferencesItem), typeof(Server.Data.LayoutPreferencesItem), preferencesItem));
		}

		public void ResetAll(Func<LayoutPreferencesDatabaseItem, bool>? predicate = null)
		{
			_database.ResetAll(item => predicate?.Invoke((LayoutPreferencesDatabaseItem)CopyValues(typeof(Server.Data.LayoutPreferences), typeof(LayoutPreferencesDatabaseItem), item)) ?? true);
		}

		public void ApplyToAll(Action<LayoutPreferencesDatabaseItem> updateAction, Func<LayoutPreferencesDatabaseItem, bool>? predicate = null)
		{
			_database.ApplyToAll(item => updateAction.Invoke((LayoutPreferencesDatabaseItem)CopyValues(typeof(Server.Data.LayoutPreferences), typeof(LayoutPreferencesDatabaseItem), item)),
				item => predicate?.Invoke((LayoutPreferencesDatabaseItem)CopyValues(typeof(Server.Data.LayoutPreferences), typeof(LayoutPreferencesDatabaseItem), item)) ?? true);
		}

		public void Import(string json)
		{
			_database.Import(json);
		}

		public string Export()
		{
			return _database.Export();
		}
	}
}
