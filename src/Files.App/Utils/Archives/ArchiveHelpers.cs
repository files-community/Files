// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Dialogs;
using Files.App.ViewModels.Dialogs;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using System.IO;
using System.Text;
using Windows.Storage;
using SevenZip;

namespace Files.App.Utils.Archives
{
	/// <summary>
	/// Provides static helper for archives, powered by <see cref="SevenZip"/>.
	/// </summary>
	public static class ArchiveHelpers
	{
		private readonly static OngoingTasksViewModel _ongoingTasksViewModel = Ioc.Default.GetRequiredService<OngoingTasksViewModel>();

		public static bool CanDecompress(IReadOnlyList<ListedItem> selectedItems)
		{
			return
				selectedItems.Any() &&
				(selectedItems.All(x => x.IsArchive) ||
				selectedItems.All(x =>
					x.PrimaryItemAttribute == StorageItemTypes.File &&
					FileExtensionHelpers.IsZipFile(x.FileExtension)));
		}

		public static bool CanCompress(IReadOnlyList<ListedItem> selectedItems)
		{
			return !CanDecompress(selectedItems) || selectedItems.Count > 1;
		}

		public static string DetermineArchiveNameFromSelection(IReadOnlyList<ListedItem> selectedItems)
		{
			if (!selectedItems.Any())
				return string.Empty;

			return
				Path.GetFileName(
					selectedItems.Count is 1
					? selectedItems[0].ItemPath
					: Path.GetDirectoryName(selectedItems[0].ItemPath))
				?? string.Empty;
		}

		public static (string[] Sources, string directory, string fileName) GetCompressDestination(IShellPage associatedInstance)
		{
			string[] sources = associatedInstance.SlimContentPage.SelectedItems
				.Select(item => item.ItemPath)
				.ToArray();

			if (sources.Length is 0)
				return (sources, string.Empty, string.Empty);

			string directory = associatedInstance.FilesystemViewModel.WorkingDirectory.Normalize();

			if (App.LibraryManager.TryGetLibrary(directory, out var library) && !library.IsEmpty)
				directory = library.DefaultSaveFolder;

			string fileName = Path.GetFileName(sources.Length is 1 ? sources[0] : directory);

			return (sources, directory, fileName);
		}

		public static async Task CompressArchiveAsync(IArchiveCreator creator)
		{
			var archivePath = creator.GetArchivePath();

			int index = 1;
			while (File.Exists(archivePath) || System.IO.Directory.Exists(archivePath))
				archivePath = creator.GetArchivePath($" ({++index})");

			creator.ArchivePath = archivePath;

			CancellationTokenSource compressionToken = new();

			PostedStatusBanner banner = _ongoingTasksViewModel.PostOperationBanner(
				"CompressionInProgress".GetLocalizedResource(),
				archivePath,
				0,
				ReturnResult.InProgress,
				FileOperationType.Compressed,
				compressionToken);

			creator.Progress = banner.ProgressEventSource;
			bool isSuccess = await creator.RunCreationAsync();

			banner.Remove();

			if (isSuccess)
			{
				_ongoingTasksViewModel.PostBanner(
					"CompressionCompleted".GetLocalizedResource(),
					string.Format("CompressionSucceded".GetLocalizedResource(), archivePath),
					0,
					ReturnResult.Success,
					FileOperationType.Compressed);
			}
			else
			{
				NativeFileOperationsHelper.DeleteFileFromApp(archivePath);

				_ongoingTasksViewModel.PostBanner(
					"CompressionCompleted".GetLocalizedResource(),
					string.Format("CompressionFailed".GetLocalizedResource(), archivePath),
					0,
					ReturnResult.Failed,
					FileOperationType.Compressed);
			}
		}

		private static async Task ExtractArchive(BaseStorageFile archive, BaseStorageFolder? destinationFolder, string password)
		{
			if (archive is null || destinationFolder is null)
				return;

			CancellationTokenSource extractCancellation = new();

			PostedStatusBanner banner = _ongoingTasksViewModel.PostOperationBanner(
				"ExtractingArchiveText".GetLocalizedResource(),
				archive.Path,
				0,
				ReturnResult.InProgress,
				FileOperationType.Extract,
				extractCancellation);

			await FilesystemTasks.Wrap(() =>
				ExtractArchive(
					archive,
					destinationFolder,
					password,
					banner.ProgressEventSource,
					extractCancellation.Token));

			banner.Remove();
			
			_ongoingTasksViewModel.PostBanner(
				"ExtractingCompleteText".GetLocalizedResource(),
				"ArchiveExtractionCompletedSuccessfullyText".GetLocalizedResource(),
				0,
				ReturnResult.Success,
				FileOperationType.Extract);
		}

		public static async Task DecompressArchive(IShellPage associatedInstance)
		{
			if (associatedInstance == null)
				return;

			BaseStorageFile archive = await StorageHelpers.ToStorageItem<BaseStorageFile>(associatedInstance.SlimContentPage.SelectedItems.Count != 0
				? associatedInstance.SlimContentPage.SelectedItem.ItemPath
				: associatedInstance.FilesystemViewModel.WorkingDirectory);

			if (archive is null)
				return;

			var isArchiveEncrypted = await FilesystemTasks.Wrap(() => IsArchiveEncrypted(archive));
			var password = string.Empty;

			DecompressArchiveDialog decompressArchiveDialog = new();
			DecompressArchiveDialogViewModel decompressArchiveViewModel = new(archive)
			{
				IsArchiveEncrypted = isArchiveEncrypted,
				ShowPathSelection = true
			};

			decompressArchiveDialog.ViewModel = decompressArchiveViewModel;

			ContentDialogResult option = await decompressArchiveDialog.TryShowAsync();

			if (option != ContentDialogResult.Primary)
				return;

			if (isArchiveEncrypted)
				password = Encoding.UTF8.GetString(decompressArchiveViewModel.Password);

			// Check if archive still exists
			if (!StorageHelpers.Exists(archive.Path))
				return;

			BaseStorageFolder destinationFolder = decompressArchiveViewModel.DestinationFolder;
			string destinationFolderPath = decompressArchiveViewModel.DestinationFolderPath;

			if (destinationFolder is null)
			{
				BaseStorageFolder parentFolder = await StorageHelpers.ToStorageItem<BaseStorageFolder>(Path.GetDirectoryName(archive.Path));
				destinationFolder = await FilesystemTasks.Wrap(() => parentFolder.CreateFolderAsync(Path.GetFileName(destinationFolderPath), CreationCollisionOption.GenerateUniqueName).AsTask());
			}

			await ExtractArchive(archive, destinationFolder, password);

			if (decompressArchiveViewModel.OpenDestinationFolderOnCompletion)
				await NavigationHelpers.OpenPath(destinationFolderPath, associatedInstance, FilesystemItemType.Directory);
		}

		public static async Task DecompressArchiveHere(IShellPage associatedInstance)
		{
			if (associatedInstance == null)
				return;

			foreach (var selectedItem in associatedInstance.SlimContentPage.SelectedItems)
			{
				var password = string.Empty;
				BaseStorageFile archive = await StorageHelpers.ToStorageItem<BaseStorageFile>(selectedItem.ItemPath);
				BaseStorageFolder currentFolder = await StorageHelpers.ToStorageItem<BaseStorageFolder>(associatedInstance.FilesystemViewModel.CurrentFolder.ItemPath);

				if (await FilesystemTasks.Wrap(() => IsArchiveEncrypted(archive)))
				{
					DecompressArchiveDialog decompressArchiveDialog = new();
					DecompressArchiveDialogViewModel decompressArchiveViewModel = new(archive)
					{
						IsArchiveEncrypted = true,
						ShowPathSelection = false
					};

					decompressArchiveDialog.ViewModel = decompressArchiveViewModel;

					ContentDialogResult option = await decompressArchiveDialog.TryShowAsync();
					if (option != ContentDialogResult.Primary)
						return;

					password = Encoding.UTF8.GetString(decompressArchiveViewModel.Password);
				}

				await ExtractArchive(archive, currentFolder, password);
			}
		}

		public static async Task DecompressArchiveToChildFolder(IShellPage associatedInstance)
		{
			if (associatedInstance == null)
				return;

			foreach (var selectedItem in associatedInstance.SlimContentPage.SelectedItems)
			{
				var password = string.Empty;

				BaseStorageFile archive = await StorageHelpers.ToStorageItem<BaseStorageFile>(selectedItem.ItemPath);
				BaseStorageFolder currentFolder = await StorageHelpers.ToStorageItem<BaseStorageFolder>(associatedInstance.FilesystemViewModel.CurrentFolder.ItemPath);
				BaseStorageFolder destinationFolder = null;

				if (await FilesystemTasks.Wrap(() => IsArchiveEncrypted(archive)))
				{
					DecompressArchiveDialog decompressArchiveDialog = new();
					DecompressArchiveDialogViewModel decompressArchiveViewModel = new(archive)
					{
						IsArchiveEncrypted = true,
						ShowPathSelection = false
					};
					decompressArchiveDialog.ViewModel = decompressArchiveViewModel;

					ContentDialogResult option = await decompressArchiveDialog.TryShowAsync();
					if (option != ContentDialogResult.Primary)
						return;

					password = Encoding.UTF8.GetString(decompressArchiveViewModel.Password);
				}

				if (currentFolder is not null)
					destinationFolder = await FilesystemTasks.Wrap(() => currentFolder.CreateFolderAsync(Path.GetFileNameWithoutExtension(archive.Path), CreationCollisionOption.GenerateUniqueName).AsTask());

				await ExtractArchive(archive, destinationFolder, password);
			}
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

		private static async Task<SevenZipExtractor?> GetZipFile(BaseStorageFile archive, string password = "")
		{
			return await FilesystemTasks.Wrap(async () =>
			{
				var arch = new SevenZipExtractor(await archive.OpenStreamForReadAsync(), password);
				return arch?.ArchiveFileData is null ? null : arch; // Force load archive (1665013614u)
			});
		}
	}
}
