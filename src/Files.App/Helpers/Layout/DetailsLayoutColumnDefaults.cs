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
	}
}
