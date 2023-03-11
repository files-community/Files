using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.Filesystem.Archive;
using Files.App.ViewModels;
using Files.App.ViewModels.UserControls;
using Files.Backend.Helpers;
using Files.Shared.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
	}
}
