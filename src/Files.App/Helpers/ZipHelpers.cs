using Files.App.Filesystem;
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
		private static async Task<SevenZipExtractor?> GetZipFile(BaseStorageFile archive, string password = "")
		{
			return await Filesystem.FilesystemTasks.Wrap(async () =>
			{
				var arch = new SevenZipExtractor(await archive.OpenStreamForReadAsync(), password);
				return arch?.ArchiveFileData is null ? null : arch; // Force load archive (1665013614u)
			});
		}

		public static async Task<bool> IsArchiveEncrypted(BaseStorageFile archive)
		{
			using SevenZipExtractor? zipFile = await GetZipFile(archive);
			if (zipFile is null)
				return true;

			return zipFile.ArchiveFileData.Any(file => file.Encrypted || file.Method.Contains("Crypto") || file.Method.Contains("AES"));
		}

		public static async Task ExtractArchive(BaseStorageFile archive, BaseStorageFolder destinationFolder, string password, IProgress<FileSystemProgress> progress, CancellationToken cancellationToken)
		{
			using SevenZipExtractor? zipFile = await GetZipFile(archive, password);
			if (zipFile is null)
				return;
			//zipFile.IsStreamOwner = true;
			var directoryEntries = new List<ArchiveFileInfo>();
			var fileEntries = new List<ArchiveFileInfo>();
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

			FileSystemProgress fsProgress = new(progress, true, Shared.Enums.FileSystemStatusCode.InProgress, entriesAmount);
			fsProgress.Report();

			foreach (var entry in fileEntries)
			{
				if (cancellationToken.IsCancellationRequested) // Check if canceled
					return;

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
				fsProgress.ProcessedItemsCount = entriesFinished;
				fsProgress.Report();
			}
		}
	}
}
