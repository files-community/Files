using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Files.App.Filesystem.Archive
{
	public interface IArchiveCreator
	{
		string ArchivePath { get; }

		string Directory { get; }
		string FileName { get; }
		string Password { get; }

		IEnumerable<string> Sources { get; }

		ArchiveFormats FileFormat { get; }
		ArchiveCompressionLevels CompressionLevel { get; }
		ArchiveSplittingSizes SplittingSize { get; }

		IProgress<float> Progress { get; set; }

		Task<bool> RunCreationAsync();
	}
}
