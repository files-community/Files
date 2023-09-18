// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.Extensions.Logging;
using SevenZip;
using System.IO;

namespace Files.App.Utils.Archives
{
	public static class DecompressHelper
	{
		private static async Task<SevenZipExtractor?> GetZipFile(BaseStorageFile archive, string password = "")
		{
			return await FilesystemTasks.Wrap(async () =>
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

		public static async Task ExtractArchive(BaseStorageFile archive, BaseStorageFolder destinationFolder, string password, IProgress<StatusCenterItemProgressModel> progress, CancellationToken cancellationToken)
		{
			using SevenZipExtractor? zipFile = await GetZipFile(archive, password);
			if (zipFile is null)
				return;

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
				App.Logger.LogWarning(ex, $"Error transforming zip names into: {destinationFolder.Path}\n" +
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
			var minimumTime = new DateTime(1);

			ulong totalSize = 0;
			foreach (var item in zipFile.ArchiveFileData)
				totalSize += item.Size;

			StatusCenterItemProgressModel fsProgress = new(
				progress,
				enumerationCompleted: true,
				FileSystemStatusCode.InProgress,
				entriesAmount,
				(long)totalSize);

			fsProgress.Report();

			foreach (var entry in fileEntries)
			{
				if (cancellationToken.IsCancellationRequested) // Check if canceled
					return;

				var filePath = destinationFolder.Path;
				foreach (var component in entry.FileName.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
					filePath = Path.Combine(filePath, component);

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
						App.Logger.LogWarning(ex, $"Error extracting file: {filePath}");
						return; // TODO: handle error
					}
				}

				_ = new FileInfo(filePath)
				{
					CreationTime = entry.CreationTime > minimumTime && entry.CreationTime < entry.LastWriteTime ? entry.CreationTime : entry.LastWriteTime,
					LastWriteTime = entry.LastWriteTime,
				};

				entriesFinished++;
				fsProgress.ProcessedItemsCount = entriesFinished;
				fsProgress.Report();
			}
		}
	}
}
