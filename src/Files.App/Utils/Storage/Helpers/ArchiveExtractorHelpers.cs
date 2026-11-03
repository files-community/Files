// Copyright (c) Files Community
// Licensed under the MIT License.

using SevenZip;
using System.IO;

namespace Files.App.Utils.Storage
{
	internal static class ArchiveExtractorHelpers
	{
		public static SevenZipExtractor Create(Stream stream, string archivePath, string password = "", Action<double>? reportProgress = null, CancellationToken cancellationToken = default)
		{
			// The returned extractor owns the stream. Metadata reads are excluded from progress;
			// reporting stays active from initialization through extraction until disposal.
			ArchiveReadProgressStream? progressStream = null;
			SevenZipExtractor? extractor = null;
			try
			{
				cancellationToken.ThrowIfCancellationRequested();
				bool isBZip2 = archivePath.EndsWith(".bz2", StringComparison.OrdinalIgnoreCase);
				if (isBZip2 && (reportProgress is not null || cancellationToken.CanBeCanceled))
					stream = progressStream = new ArchiveReadProgressStream(stream, reportProgress ?? (_ => { }), cancellationToken);

				// Signature detection rejects valid 14-byte empty BZip2 streams.
				extractor = isBZip2
					? new SevenZipExtractor(stream, password, false, InArchiveFormat.BZip2)
					: new SevenZipExtractor(stream, password);
				_ = extractor.ArchiveFileData ?? throw new InvalidDataException("Archive metadata is unavailable.");
				cancellationToken.ThrowIfCancellationRequested();
				progressStream?.StartReporting();
				return extractor;
			}
			catch
			{
				if (extractor is not null)
					extractor.Dispose();
				else
					stream.Dispose();
				throw;
			}
		}
	}
}
