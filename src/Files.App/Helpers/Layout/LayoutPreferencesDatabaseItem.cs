// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Helpers
{
	/// <summary>
	/// Represents item for the database of a folder's layout preferences.
	/// </summary>
	public sealed class LayoutPreferencesDatabaseItem
	{
		public ulong? Frn { get; set; }

		public string FilePath { get; set; } = string.Empty;

		public LayoutPreferencesItem LayoutPreferencesManager { get; set; } = new();
	}
}
