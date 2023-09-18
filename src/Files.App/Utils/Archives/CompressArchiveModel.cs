// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.Extensions.Logging;
using SevenZip;
using System.IO;

namespace Files.App.Utils.Archives
{
	/// <summary>
	/// Provides an archive creation support.
	/// </summary>
	public class CompressArchiveModel : ICompressArchiveModel
	{
		/// <summary>
		/// Represents the total number of items to be processed.
		/// </summary>
		/// <remarks>
		/// It is used to calculate a weighted progress with this formula:
		/// <code>Progress = [OldProgress + (ProgressDelta / ItemsAmount)]</code>
		/// </remarks>
		private int _itemsAmount = 1;

		private int _processedItems = 0;

		private StatusCenterItemProgressModel _fileSystemProgress;

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

		private IProgress<StatusCenterItemProgressModel> _Progress;
		public IProgress<StatusCenterItemProgressModel> Progress
		{
			get => _Progress;
			set
			{
				_Progress = value;

				long totalSize = 0;
				
				if (Sources is not null)
					totalSize = Sources.Select(FileOperationsHelpers.GetFileSize).Sum();

				_fileSystemProgress = new(
					Progress,
					true,
					FileSystemStatusCode.InProgress,
					Sources is null ? 0 : Sources.Count(),
					totalSize);

				_fileSystemProgress.Report(0);
			}
		}

		/// <inheritdoc/>
		public string ArchivePath { get; set; }

		/// <inheritdoc/>
		public string Directory { get; init; }

		/// <inheritdoc/>
		public string FileName { get; init; }

		/// <inheritdoc/>
		public string Password { get; init; }

		/// <inheritdoc/>
		public IEnumerable<string> Sources { get; init; }

		/// <inheritdoc/>
		public ArchiveFormats FileFormat { get; init; }

		/// <inheritdoc/>
		public ArchiveCompressionLevels CompressionLevel { get; init; }

		/// <inheritdoc/>
		public ArchiveSplittingSizes SplittingSize { get; init; }

		public CompressArchiveModel(
			string[] source,
			string directory,
			string fileName,
			string? password = null,
			ArchiveFormats fileFormat = ArchiveFormats.Zip,
			ArchiveCompressionLevels compressionLevel = ArchiveCompressionLevels.Normal,
			ArchiveSplittingSizes splittingSize = ArchiveSplittingSizes.None)
		{
			_Progress = new Progress<StatusCenterItemProgressModel>();

			Sources = source;
			Directory = directory;
			FileName = fileName;
			Password = password ?? string.Empty;
			ArchivePath = string.Empty;
			FileFormat = fileFormat;
			CompressionLevel = compressionLevel;
			SplittingSize = splittingSize;

			_fileSystemProgress.Report(0);
		}

		/// <inheritdoc/>
		public string GetArchivePath(string suffix = "")
		{
			return Path.Combine(Directory, $"{FileName}{suffix}{ArchiveExtension}");
		}

		/// <inheritdoc/>
		public async Task<bool> RunCreationAsync()
		{
			string[] sources = Sources.ToArray();

			var compressor = new SevenZipCompressor()
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
				var files = sources.Where(File.Exists).ToArray();
				var directories = sources.Where(SystemIO.Directory.Exists);

				_itemsAmount = files.Length + directories.Count();

				foreach (string directory in directories)
				{
					await compressor.CompressDirectoryAsync(directory, ArchivePath, Password);

					compressor.CompressionMode = CompressionMode.Append;
				}

				if (files.Any())
				{
					if (string.IsNullOrEmpty(Password))
						await compressor.CompressFilesAsync(ArchivePath, files);
					else
						await compressor.CompressFilesEncryptedAsync(ArchivePath, Password, files);
				}

				return true;
			}
			catch (Exception ex)
			{
				var logger = Ioc.Default.GetRequiredService<ILogger<App>>();
				logger?.LogWarning(ex, $"Error compressing folder: {ArchivePath}");

				return false;
			}
		}

		private void Compressor_CompressionFinished(object? sender, EventArgs e)
		{
			if (++_processedItems == _itemsAmount)
			{
				_fileSystemProgress.ReportStatus(FileSystemStatusCode.Success);
			}
			else
			{
				_fileSystemProgress.Report(_processedItems * 100.0 / _itemsAmount);
			}
		}

		private void Compressor_Compressing(object? _, ProgressEventArgs e)
		{
			_fileSystemProgress.Report((double)e.PercentDelta / _itemsAmount);
		}
	}
}
