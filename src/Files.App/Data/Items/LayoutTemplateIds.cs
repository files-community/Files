// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

namespace Files.App.Data.Items
{
	/// <summary>
	/// Provides the built-in layout templates.
	/// </summary>
	public static class LayoutTemplateIds
	{
		public const string Default = "Default";
		public const string GroupedByDate = "GroupedByDate";
		public const string PicturesAndVideos = "PicturesAndVideos";
		public const string Music = "Music";
		public const string RecycleBin = "RecycleBin";

		public static IReadOnlyList<string> BuiltIn { get; } = [Default, GroupedByDate, PicturesAndVideos, Music, RecycleBin];

		public static bool IsBuiltIn(string id)
			=> BuiltIn.Contains(id);

		public static string GetBuiltInName(string id)
		{
			return id switch
			{
				Default => Strings.Documents.GetLocalizedResource(),
				GroupedByDate => Strings.FilesByDate.GetLocalizedResource(),
				PicturesAndVideos => Strings.ImagesAndVideos.GetLocalizedResource(),
				Music => Strings.AudioFiles.GetLocalizedResource(),
				RecycleBin => Strings.DeletedFiles.GetLocalizedResource(),
				_ => id,
			};
		}

		public static LayoutTemplate CreateBuiltIn(string id)
		{
			return id switch
			{
				GroupedByDate => new()
				{
					Id = id,
					GroupOption = GroupOption.DateCreated,
					GroupDirection = SortDirection.Descending,
				},
				PicturesAndVideos => new()
				{
					Id = id,
					LayoutMode = FolderLayoutModes.GridView,
					Columns = [DetailsLayoutColumnIds.DateModified, "System.Image.Dimensions", DetailsLayoutColumnIds.Size, "System.Media.Duration"],
				},
				Music => new()
				{
					Id = id,
					Columns = ["System.Music.TrackNumber", "System.Title", "System.Music.Artist", "System.Music.AlbumTitle", "System.Media.Duration"],
				},
				RecycleBin => new()
				{
					Id = id,
					Columns = [DetailsLayoutColumnIds.OriginalPath, DetailsLayoutColumnIds.DateDeleted, DetailsLayoutColumnIds.Size, DetailsLayoutColumnIds.ItemType, DetailsLayoutColumnIds.DateModified],
				},
				_ => new() { Id = id },
			};
		}
	}
}
