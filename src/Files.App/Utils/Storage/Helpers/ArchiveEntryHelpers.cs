// Copyright (c) Files Community
// Licensed under the MIT License.

using SevenZip;
using IO = System.IO;

namespace Files.App.Utils.Storage
{
	public static class ArchiveEntryHelpers
	{
		// SevenZipSharp reports this placeholder for entries with no stored name,
		// e.g. gzip archives created without the optional FNAME header field.
		private const string NamelessEntryPlaceholder = "[no name]";

		/// <summary>
		/// Gets the archive's entries with each nameless entry renamed via <see cref="GetEntryName"/>.
		/// </summary>
		public static IEnumerable<ArchiveFileInfo> GetArchiveFileData(this SevenZipExtractor extractor, string containerPath)
		{
			return extractor.ArchiveFileData.Select(entry =>
			{
				entry.FileName = GetEntryName(entry, containerPath);
				return entry;
			});
		}

		/// <summary>
		/// Gets the name of an archive entry, substituting the archive's own file name
		/// without its extension when the entry has no stored name, as 7-Zip does.
		/// </summary>
		public static string GetEntryName(ArchiveFileInfo entry, string containerPath)
		{
			if (!string.IsNullOrEmpty(entry.FileName) && entry.FileName != NamelessEntryPlaceholder)
				return entry.FileName;

			var fallback = IO.Path.GetFileNameWithoutExtension(containerPath);
			return string.IsNullOrEmpty(fallback) ? NamelessEntryPlaceholder : fallback;
		}
	}
}
