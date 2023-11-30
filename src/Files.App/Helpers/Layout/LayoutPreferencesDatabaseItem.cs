// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using LiteDB;

namespace Files.App.Helpers
{
	public class LayoutPreferencesDatabaseItem
	{
		[BsonId]
		public int Id { get; set; }

		public ulong? Frn { get; set; }

		public string FilePath { get; set; } = string.Empty;

		public LayoutPreferencesManager LayoutPreferencesManager { get; set; } = LayoutPreferencesManager.DefaultLayoutPreferences;
	}
}
