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
		public const string GitRepository = "GitRepository";
		public const string RecycleBin = "RecycleBin";

		public static IReadOnlyList<string> BuiltIn { get; } = new[] { Default, GroupedByDate, PicturesAndVideos, Music, GitRepository, RecycleBin };

		public static bool IsBuiltIn(string id)
			=> BuiltIn.Contains(id);

		public static string GetBuiltInName(string id)
		{
			return id switch
			{
				Default => Strings.Default.GetLocalizedResource(),
				GroupedByDate => Strings.FilesByDate.GetLocalizedResource(),
				PicturesAndVideos => Strings.ImagesAndVideos.GetLocalizedResource(),
				Music => Strings.AudioFiles.GetLocalizedResource(),
				GitRepository => Strings.GitRepository.GetLocalizedResource(),
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
					GroupOption = GroupOption.DateModified,
					GroupDirection = SortDirection.Descending,
				},
				PicturesAndVideos => new()
				{
					Id = id,
					LayoutMode = FolderLayoutModes.GridView,
				},
				Music => new()
				{
					Id = id,
					Columns = ["System.Music.TrackNumber", "System.Title", "System.Music.Artist", "System.Music.AlbumTitle", "System.Media.Duration"],
				},
				GitRepository => new()
				{
					Id = id,
					Columns = [DetailsLayoutColumnIds.GitStatus, DetailsLayoutColumnIds.GitLastCommitMessage, DetailsLayoutColumnIds.GitLastCommitDate, DetailsLayoutColumnIds.GitCommitAuthor, DetailsLayoutColumnIds.DateModified, DetailsLayoutColumnIds.Size],
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
