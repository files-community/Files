using Microsoft.Extensions.Logging;
using SevenZip;

namespace Files.App.Filesystem.Archive
{
	public class ArchiveCreator : IArchiveCreator
	{
		// Represents the total number of items to be processed.
		// It is used to calculate a weighted progress with this formula:
		// Progress = [OldProgress + (ProgressDelta / ItemsAmount)]
		private int itemsAmount = 1;
		private int processedItems = 0;

		private string archivePath = string.Empty;
		public string ArchivePath
		{
			get => archivePath;
			set => archivePath = value;
		}

		public string Directory { get; init; } = string.Empty;
		public string FileName { get; init; } = string.Empty;
		public string Password { get; init; } = string.Empty;

		public IEnumerable<string> Sources { get; init; } = Enumerable.Empty<string>();

		public ArchiveFormats FileFormat { get; init; } = ArchiveFormats.Zip;
		public ArchiveCompressionLevels CompressionLevel { get; init; } = ArchiveCompressionLevels.Normal;
		public ArchiveSplittingSizes SplittingSize { get; init; } = ArchiveSplittingSizes.None;

		private IProgress<FileSystemProgress> progress = new Progress<FileSystemProgress>();
		public IProgress<FileSystemProgress> Progress
		{
			get => progress;
			set
			{
				progress = value;
				fsProgress = new(Progress, true, Shared.Enums.FileSystemStatusCode.InProgress);
				fsProgress.Report(0);
			}
		}

		private FileSystemProgress fsProgress;

		public ArchiveCreator()
		{
			fsProgress = new(Progress, true, Shared.Enums.FileSystemStatusCode.InProgress);
			fsProgress.Report(0);
		}

		private string ArchiveExtension => FileFormat switch
		{
			ArchiveFormats.Zip => ".zip",
			ArchiveFormats.SevenZip => ".7z",
			ArchiveFormats.Tar => ".tar",
			ArchiveFormats.GZip => ".gz",
			_ => throw new ArgumentOutOfRangeException(nameof(FileFormat)),
		};
		private OutArchiveFormat SevenZipArchiveFormat => FileFormat switch
		{
			ArchiveFormats.Zip => OutArchiveFormat.Zip,
			ArchiveFormats.SevenZip => OutArchiveFormat.SevenZip,
			ArchiveFormats.Tar => OutArchiveFormat.Tar,
			ArchiveFormats.GZip => OutArchiveFormat.GZip,
			_ => throw new ArgumentOutOfRangeException(nameof(FileFormat)),
		};
		private CompressionLevel SevenZipCompressionLevel => CompressionLevel switch
		{
			ArchiveCompressionLevels.Ultra => SevenZip.CompressionLevel.Ultra,
			ArchiveCompressionLevels.High => SevenZip.CompressionLevel.High,
			ArchiveCompressionLevels.Normal => SevenZip.CompressionLevel.Normal,
			ArchiveCompressionLevels.Low => SevenZip.CompressionLevel.Low,
			ArchiveCompressionLevels.Fast => SevenZip.CompressionLevel.Fast,
			ArchiveCompressionLevels.None => SevenZip.CompressionLevel.None,
			_ => throw new ArgumentOutOfRangeException(nameof(CompressionLevel)),
		};
		private long SevenZipVolumeSize => SplittingSize switch
		{
			ArchiveSplittingSizes.None => 0L,
			ArchiveSplittingSizes.Mo10 => 10 * 1000 * 1000L,
			ArchiveSplittingSizes.Mo100 => 100 * 1000 * 1000L,
			ArchiveSplittingSizes.Mo1024 => 1024 * 1000 * 1000L,
			ArchiveSplittingSizes.Mo5120 => 5120 * 1000 * 1000L,
			ArchiveSplittingSizes.Fat4092 => 4092 * 1000 * 1000L,
			ArchiveSplittingSizes.Cd650 => 650 * 1000 * 1000L,
			ArchiveSplittingSizes.Cd700 => 700 * 1000 * 1000L,
			ArchiveSplittingSizes.Dvd4480 => 4480 * 1000 * 1000L,
			ArchiveSplittingSizes.Dvd8128 => 8128 * 1000 * 1000L,
			ArchiveSplittingSizes.Bd23040 => 23040 * 1000 * 1000L,
			_ => throw new ArgumentOutOfRangeException(nameof(SplittingSize)),
		};

		public string GetArchivePath(string suffix = "")
		{
			return Path.Combine(Directory, $"{FileName}{suffix}{ArchiveExtension}");
		}

		public async Task<bool> RunCreationAsync()
		{
			string[] sources = Sources.ToArray();

			var compressor = new SevenZipCompressor
			{
				ArchiveFormat = SevenZipArchiveFormat,
				CompressionLevel = SevenZipCompressionLevel,
				VolumeSize = FileFormat is ArchiveFormats.SevenZip ? SevenZipVolumeSize : 0,
				FastCompression = false,
				IncludeEmptyDirectories = true,
				EncryptHeaders = true,
				PreserveDirectoryRoot = sources.Length > 1,
				EventSynchronization = EventSynchronizationStrategy.AlwaysAsynchronous,
			};
			compressor.Compressing += Compressor_Compressing;
			compressor.CompressionFinished += Compressor_CompressionFinished;

			try
			{
				var files = sources.Where(source => File.Exists(source)).ToArray();
				var directories = sources.Where(source => System.IO.Directory.Exists(source));

				itemsAmount = files.Length + directories.Count();

				foreach (string directory in directories)
				{
					await compressor.CompressDirectoryAsync(directory, archivePath, Password);
					compressor.CompressionMode = CompressionMode.Append;
				}

				if (files.Any())
				{
					if (string.IsNullOrEmpty(Password))
						await compressor.CompressFilesAsync(archivePath, files);
					else
						await compressor.CompressFilesEncryptedAsync(archivePath, Password, files);
				}

				return true;
			}
			catch (Exception ex)
			{
				var logger = Ioc.Default.GetRequiredService<ILogger<App>>();
				logger?.LogWarning(ex, $"Error compressing folder: {archivePath}");

				return false;
			}
		}

		private void Compressor_CompressionFinished(object? sender, EventArgs e)
		{
			if (++processedItems == itemsAmount)
			{
				fsProgress.Percentage = null;
				fsProgress.ReportStatus(Shared.Enums.FileSystemStatusCode.Success);
			}
			else
			{
				fsProgress.Percentage = processedItems * 100 / itemsAmount;
				fsProgress.Report(fsProgress.Percentage);
			}
		}

		private void Compressor_Compressing(object? _, ProgressEventArgs e)
		{
			fsProgress.Percentage += e.PercentDelta / itemsAmount;
			fsProgress.Report(fsProgress.Percentage);
		}
	}
}
