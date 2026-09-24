$\xEF\xBB\xBF// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Items
{
	public enum BookmarkKind
	{
		Folder,
		File,
		App,
		Group
	}

	/// <summary>
	/// A single entry on the bookmarks bar. Kept as a plain type so it can be source-generated for JSON.
	/// </summary>
	public sealed class BookmarkItem
	{
		public string Title { get; set; } = string.Empty;

		public BookmarkKind Kind { get; set; }

		/// <summary>
		/// Gets or sets the folder, file or executable path. Null for groups.
		/// </summary>
		public string? Path { get; set; }

		/// <summary>
		/// Gets or sets the command line arguments used when launching an app.
		/// </summary>
		public string? Arguments { get; set; }

		/// <summary>
		/// Gets or sets the nested bookmarks of a group.
		/// </summary>
		public List<BookmarkItem> Children { get; set; } = [];

		[JsonIgnore]
		public bool IsGroup
			=> Kind is BookmarkKind.Group;

		[JsonIgnore]
		public string Glyph => Kind switch
		{
			BookmarkKind.Folder => "\uE8B7",
			BookmarkKind.Group => "\uED25",
			BookmarkKind.App => "\uE71D",
			_ => "\uE8A5"
		};
	}
}
