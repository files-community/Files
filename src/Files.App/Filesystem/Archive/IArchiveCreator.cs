namespace Files.App.Filesystem.Archive
{
	public interface IArchiveCreator
	{
		string ArchivePath { get; set; }

		string Directory { get; }
		string FileName { get; }
		string Password { get; }

		IEnumerable<string> Sources { get; }

		ArchiveFormats FileFormat { get; }
		ArchiveCompressionLevels CompressionLevel { get; }
		ArchiveSplittingSizes SplittingSize { get; }

		IProgress<FileSystemProgress> Progress { get; set; }

		string GetArchivePath(string suffix = "");

		Task<bool> RunCreationAsync();
	}
}
