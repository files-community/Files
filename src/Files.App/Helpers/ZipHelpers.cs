using Files.App.Filesystem.StorageItems;
using SevenZip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Files.App.Helpers
{
	public static class ZipHelpers
	{
		public static async Task<bool> CompressMultipleToArchive(string[] sourceFolders, string archive, IProgress<float> progressDelegate)
		{
			SevenZipCompressor compressor = new()
			{
				ArchiveFormat = OutArchiveFormat.Zip,
				CompressionLevel = CompressionLevel.High,
				EventSynchronization = EventSynchronizationStrategy.AlwaysAsynchronous,
				FastCompression = true,
				IncludeEmptyDirectories = true,
				PreserveDirectoryRoot = sourceFolders.Length > 1
			};

			bool noErrors = true;
			try
			{
				for (int i = 0; i < sourceFolders.Length; i++)
				{
					if (i > 0)
						compressor.CompressionMode = CompressionMode.Append;

					await compressor.CompressDirectoryAsync(sourceFolders[i], archive);
					float percentage = (i + 1.0f) / sourceFolders.Length * 100.0f;
					progressDelegate?.Report(percentage);
				}
			}
			catch (Exception ex)
			{
				App.Logger.Warn(ex, $"Error compressing folder: {archive}");
				NativeFileOperationsHelper.DeleteFileFromApp(archive);
				noErrors = false;
			}
			return noErrors;
		}

		public static async Task ExtractArchive(BaseStorageFile archive, BaseStorageFolder destinationFolder, IProgress<float> progressDelegate, CancellationToken cancellationToken)
		{
			using (SevenZipExtractor zipFile = await Filesystem.FilesystemTasks.Wrap(async () =>
			{
				var arch = new SevenZipExtractor(await archive.OpenStreamForReadAsync());
				return arch?.ArchiveFileData is null ? null : arch; // Force load archive (1665013614u)
			}))
			{
				if (zipFile is null)
					return;
				//zipFile.IsStreamOwner = true;
				List<ArchiveFileInfo> directoryEntries = new List<ArchiveFileInfo>();
				List<ArchiveFileInfo> fileEntries = new List<ArchiveFileInfo>();
				foreach (ArchiveFileInfo entry in zipFile.ArchiveFileData)
				{
					if (!entry.IsDirectory)
						fileEntries.Add(entry);
					else
						directoryEntries.Add(entry);
				}

				if (cancellationToken.IsCancellationRequested) // Check if cancelled
					return;

				var directories = new List<string>();
				try
				{
					directories.AddRange(directoryEntries.Select((entry) => entry.FileName));
					directories.AddRange(fileEntries.Select((entry) => Path.GetDirectoryName(entry.FileName)));
				}
				catch (Exception ex)
				{
					App.Logger.Warn(ex, $"Error transforming zip names into: {destinationFolder.Path}\n" +
						$"Directories: {string.Join(", ", directoryEntries.Select(x => x.FileName))}\n" +
						$"Files: {string.Join(", ", fileEntries.Select(x => x.FileName))}");
					return;
				}

				foreach (var dir in directories.Distinct().OrderBy(x => x.Length))
				{
					if (!NativeFileOperationsHelper.CreateDirectoryFromApp(dir, IntPtr.Zero))
					{
						var dirName = destinationFolder.Path;
						foreach (var component in dir.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
						{
							dirName = Path.Combine(dirName, component);
							NativeFileOperationsHelper.CreateDirectoryFromApp(dirName, IntPtr.Zero);
						}
					}

					if (cancellationToken.IsCancellationRequested) // Check if canceled
						return;
				}

				if (cancellationToken.IsCancellationRequested) // Check if canceled
					return;

				// Fill files

				byte[] buffer = new byte[4096];
				int entriesAmount = fileEntries.Count;
				int entriesFinished = 0;

				foreach (var entry in fileEntries)
				{
					if (cancellationToken.IsCancellationRequested) // Check if canceled
						return;
					if (entry.Encrypted)
					{
						App.Logger.Info($"Skipped encrypted zip entry: {entry.FileName}");
						continue; // TODO: support password protected archives
					}

					string filePath = Path.Combine(destinationFolder.Path, entry.FileName);

					var hFile = NativeFileOperationsHelper.CreateFileForWrite(filePath);
					if (hFile.IsInvalid)
						return; // TODO: handle error

					// We don't close hFile because FileStream.Dispose() already does that
					using (FileStream destinationStream = new FileStream(hFile, FileAccess.Write))
					{
						try
						{
							await zipFile.ExtractFileAsync(entry.Index, destinationStream);
						}
						catch (Exception ex)
						{
							App.Logger.Warn(ex, $"Error extracting file: {filePath}");
							return; // TODO: handle error
						}
					}

					entriesFinished++;
					float percentage = (float)((float)entriesFinished / (float)entriesAmount) * 100.0f;
					progressDelegate?.Report(percentage);
				}
			}
		}
	}
}