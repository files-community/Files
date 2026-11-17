// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

namespace Files.App.Helpers
{
	/// <summary>
	/// Provides what folders contain.
	/// </summary>
	public static class FolderContentHelpers
	{
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
