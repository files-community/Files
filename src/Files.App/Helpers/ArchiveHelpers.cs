using Files.App.Dialogs;
using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.Filesystem.Archive;
using Files.App.Filesystem.StorageItems;
using Files.App.ViewModels;
using Files.App.ViewModels.Dialogs;
using Files.Backend.Helpers;
using Files.Shared.Enums;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace Files.App.Helpers
{
	public static class ArchiveHelpers
	{
		public static bool CanDecompress(IReadOnlyList<ListedItem> selectedItems)
		{
			return selectedItems.Any() && selectedItems.All(x => x.IsArchive)
				|| selectedItems.All(x => x.PrimaryItemAttribute == StorageItemTypes.File && FileExtensionHelpers.IsZipFile(x.FileExtension));
		}

		public static bool CanCompress(IReadOnlyList<ListedItem> selectedItems)
		{
			return !CanDecompress(selectedItems) || selectedItems.Count > 1;
		}

		public static string DetermineArchiveNameFromSelection(IReadOnlyList<ListedItem> selectedItems)
		{
			if (!selectedItems.Any())
				return string.Empty;

			return Path.GetFileName(
					selectedItems.Count is 1
					? selectedItems[0].ItemPath
					: Path.GetDirectoryName(selectedItems[0].ItemPath
				)) ?? string.Empty;
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
			var archivePath = creator.ArchivePath;

			CancellationTokenSource compressionToken = new();
			PostedStatusBanner banner = App.OngoingTasksViewModel.PostOperationBanner
			(
				"CompressionInProgress".GetLocalizedResource(),
				archivePath,
				0,
				ReturnResult.InProgress,
				FileOperationType.Compressed,
				compressionToken
			);

			creator.Progress = banner.ProgressEventSource;
			bool isSuccess = await creator.RunCreationAsync();

			banner.Remove();

			if (isSuccess)
			{
				App.OngoingTasksViewModel.PostBanner
				(
					"CompressionCompleted".GetLocalizedResource(),
					string.Format("CompressionSucceded".GetLocalizedResource(), archivePath),
					0,
					ReturnResult.Success,
					FileOperationType.Compressed
				);
			}
			else
			{
				NativeFileOperationsHelper.DeleteFileFromApp(archivePath);

				App.OngoingTasksViewModel.PostBanner
				(
					"CompressionCompleted".GetLocalizedResource(),
					string.Format("CompressionFailed".GetLocalizedResource(), archivePath),
					0,
					ReturnResult.Failed,
					FileOperationType.Compressed
				);
			}
		}

		public static async Task DecompressArchive(IShellPage associatedInstance)
		{
			BaseStorageFile archive = await StorageHelpers.ToStorageItem<BaseStorageFile>(associatedInstance.SlimContentPage.SelectedItems.Count != 0
				? associatedInstance.SlimContentPage.SelectedItem.ItemPath
				: associatedInstance.FilesystemViewModel.WorkingDirectory);

			if (archive is null)
				return;

			var isArchiveEncrypted = await FilesystemTasks.Wrap(() => ZipHelpers.IsArchiveEncrypted(archive));
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

		private static async Task ExtractArchive(BaseStorageFile archive, BaseStorageFolder? destinationFolder, string password)
		{
			if (archive is null || destinationFolder is null)
				return;

			CancellationTokenSource extractCancellation = new();

			PostedStatusBanner banner = App.OngoingTasksViewModel.PostOperationBanner(
				archive.Name.Length >= 30 ? archive.Name + "\n" : archive.Name,
				"ExtractingArchiveText".GetLocalizedResource(),
				0,
				ReturnResult.InProgress,
				FileOperationType.Extract,
				extractCancellation);

			Stopwatch sw = new();
			sw.Start();

			await FilesystemTasks.Wrap(() => ZipHelpers.ExtractArchive(archive, destinationFolder, password, banner.ProgressEventSource, extractCancellation.Token));

			sw.Stop();
			banner.Remove();

			if (sw.Elapsed.TotalSeconds >= 6)
			{
				App.OngoingTasksViewModel.PostBanner(
					"ExtractingCompleteText".GetLocalizedResource(),
					"ArchiveExtractionCompletedSuccessfullyText".GetLocalizedResource(),
					0,
					ReturnResult.Success,
					FileOperationType.Extract);
			}
		}
	}
}
