// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Filesystem.Archive;

namespace Files.App.Data.Items
{
	public record SevenZipSplittingSizeItem(ArchiveSplittingSizes Key, string Label, string Description = "")
	{
		public string Separator
			=> string.IsNullOrEmpty(Description) ? string.Empty : "-";
	}
}
