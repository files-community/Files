// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

namespace Files.App.Helpers
{
	/// <summary>
	/// Provides the display names of <see cref="DetailsLayoutPage"/> columns.
	/// </summary>
	public static class DetailsLayoutColumnDefaults
	{
		public static string GetDisplayName(string id, IReadOnlyDictionary<string, ShellPropertyColumnInfo>? catalog)
		{
			if (DetailsLayoutColumnDefinitions.IsBuiltIn(id))
				return DetailsLayoutColumnDefinitions.GetHeader(id);

			if (id == "System.Keywords")
				return Strings.FileExplorerTags.GetLocalizedResource();

			return catalog is not null && catalog.TryGetValue(id, out var info) ? info.DisplayName : id;
		}

		/// <summary>
		/// Gets the entry that saves a default property column with its width.
		/// </summary>
		public static string FormatDefaultColumn(string id, double width)
			=> string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{id}|{width}");

		/// <summary>
		/// Reads a saved default property column, which has no width when it was chosen in Settings.
		/// </summary>
		public static (string Id, double? Width) ParseDefaultColumn(string entry)
		{
			var separator = entry.LastIndexOf('|');
			if (separator > 0 && double.TryParse(entry.AsSpan(separator + 1), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var width))
				return (entry[..separator], width);

			return (entry, null);
		}
	}
}
