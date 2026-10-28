// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Helpers
{
	/// <summary>
	/// Provides what folders contain, for the file type rules of layout templates.
	/// </summary>
	public static class FolderContentHelpers
	{
		private const int MaxSampledItems = 200;

		/// <summary>
		/// Reads a small sample of the items of a folder on a fixed drive, so navigation opens in the right layout without a switch after loading.
		/// </summary>
		public static LayoutFolderContent? ReadContent(string? path)
		{
			if (path is null)
				return null;

			try
			{
				var root = SystemIO.Path.GetPathRoot(path);
				if (string.IsNullOrEmpty(root) || root.StartsWith(@"\\", StringComparison.Ordinal) ||
					new SystemIO.DriveInfo(root).DriveType is not SystemIO.DriveType.Fixed)
					return null;

				return Count(new SystemIO.DirectoryInfo(path).EnumerateFileSystemInfos().Take(MaxSampledItems).Select(info => info is SystemIO.FileInfo ? info.Extension : null));
			}
			catch (Exception ex) when (ex is SystemIO.IOException or UnauthorizedAccessException or ArgumentException)
			{
				return null;
			}
		}

		/// <summary>
		/// Gets what a loaded folder contains.
		/// </summary>
		public static LayoutFolderContent GetContent(IList<ListedItem> items)
			=> Count(items.Select(item => item.FileExtension));

		private static LayoutFolderContent Count(IEnumerable<string?> extensions)
		{
			var total = 0;
			var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			foreach (var extension in extensions)
			{
				total++;
				if (!string.IsNullOrEmpty(extension))
					counts[extension] = counts.GetValueOrDefault(extension) + 1;
			}

			return new(total, counts);
		}
	}
}
