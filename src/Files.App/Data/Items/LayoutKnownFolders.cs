// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Windows.Storage;

namespace Files.App.Data.Items
{
	/// <summary>
	/// Provides the user folders that layout rules can refer to by name, so the rules follow the folders when they move.
	/// </summary>
	public static class LayoutKnownFolders
	{
		public const string Desktop = "Desktop";
		public const string Documents = "Documents";
		public const string Downloads = "Downloads";
		public const string Music = "Music";
		public const string Pictures = "Pictures";
		public const string Videos = "Videos";

		public static IReadOnlyList<string> All { get; } = [Desktop, Documents, Downloads, Music, Pictures, Videos];

		private static readonly Lazy<Dictionary<string, string>> _paths = new(() =>
		{
			var paths = UserDataPaths.GetDefault();
			return new()
			{
				[Desktop] = paths.Desktop,
				[Documents] = paths.Documents,
				[Downloads] = paths.Downloads,
				[Music] = paths.Music,
				[Pictures] = paths.Pictures,
				[Videos] = paths.Videos,
			};
		});

		public static bool IsKnownFolder(string value)
			=> All.Contains(value);

		/// <summary>
		/// Gets the path of a rule folder, resolving known folder names.
		/// </summary>
		public static string GetPath(string? value)
			=> value is null ? string.Empty : _paths.Value.TryGetValue(value, out var path) ? path : value;

		public static string GetDisplayName(string value)
		{
			return value switch
			{
				Desktop => Strings.Desktop.GetLocalizedResource(),
				Documents => Strings.Documents.GetLocalizedResource(),
				Downloads => Strings.Downloads.GetLocalizedResource(),
				Music => Strings.Music.GetLocalizedResource(),
				Pictures => Strings.Pictures.GetLocalizedResource(),
				Videos => Strings.Videos.GetLocalizedResource(),
				_ => value,
			};
		}
	}
}
