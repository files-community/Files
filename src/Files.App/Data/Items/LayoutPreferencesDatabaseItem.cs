// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Helpers.LayoutPreferences;
using LiteDB;

namespace Files.App.Data.Items
{
	internal class LayoutPreferencesDatabaseItem
	{
		[BsonId]
		public int Id { get; set; }

		public ulong? Frn { get; set; }

		public string FilePath { get; set; } = string.Empty;

		public LayoutPreferencesModel Prefs { get; set; } = LayoutPreferencesModel.DefaultLayoutPreferences;
	}
}
