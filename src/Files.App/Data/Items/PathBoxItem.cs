// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Items
{
	public sealed class PathBoxItem
	{
		public string? Title { get; set; }

		public string? Path { get; set; }

		public string? ChevronToolTip { get; set; }

		// Only enumerable for fully qualified filesystem paths. Excludes the search-results
		// placeholder (Path == null), shell virtual locations ("Shell:..." / "Home" / "Settings"
		// / "ReleaseNotes"), and other non-fileystem entries that GetSubfolders can't enumerate.
		public bool IsChevronVisible =>
			!string.IsNullOrEmpty(Path) && SystemIO.Path.IsPathFullyQualified(Path);
	}
}
