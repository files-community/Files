// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using LiteDB;

namespace Files.App.Helpers
{
	/// <summary>
	/// Represents item for the database of a folder's layout preferences.
	/// </summary>
	public class LayoutPreferencesDatabaseItem
	{
		[BsonId]
		public int Id { get; set; }

		public ulong? Frn { get; set; }

		public string FilePath { get; set; } = string.Empty;

		public LayoutPreferencesItem LayoutPreferencesManager { get; set; } = new();
	}
}
