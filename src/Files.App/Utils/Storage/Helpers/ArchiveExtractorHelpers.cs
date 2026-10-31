// Copyright (c) Files Community
// Licensed under the MIT License.

using SevenZip;
using System.IO;

namespace Files.App.Utils.Storage
{
	internal static class ArchiveExtractorHelpers
	{
		public static SevenZipExtractor Create(Stream stream, string archivePath, string password = "")
		{
			// Signature detection rejects valid empty BZip2 streams because they are only 14 bytes long.
			return archivePath.EndsWith(".bz2", StringComparison.OrdinalIgnoreCase)
				? new SevenZipExtractor(stream, password, false, InArchiveFormat.BZip2)
				: new SevenZipExtractor(stream, password);
		}
	}
}
