// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Dialogs;
using Files.App.ViewModels.Dialogs;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using SevenZip;
using System.IO;
using System.Text;
using Windows.Foundation.Metadata;
using Windows.Storage;

namespace Files.App.Utils.Archives
{
	public static class DecompressHelper
	{
		private readonly static StatusCenterViewModel _statusCenterViewModel = Ioc.Default.GetRequiredService<StatusCenterViewModel>();

		private static IThreadingService _threadingService = Ioc.Default.GetRequiredService<IThreadingService>();

		public static async Task<bool> DecompressArchiveAsync(BaseStorageFile archive, BaseStorageFolder destinationFolder, string password, IProgress<StatusCenterItemProgressModel> progress, CancellationToken cancellationToken)
		{
			using SevenZipExtractor? zipFile = await GetZipFile(archive, password);
			if (zipFile is null)
				return false;

			// Check if the decompress operation canceled
			if (cancellationToken.IsCancellationRequested)
				return false;

			// Fill files

			byte[] buffer = new byte[4096];
			int entriesAmount = zipFile.ArchiveFileData.Where(x => !x.IsDirectory).Count();

			StatusCenterItemProgressModel fsProgress = new(
				progress,
				enumerationCompleted: true,
				FileSystemStatusCode.InProgress,
				entriesAmount);

			fsProgress.TotalSize = zipFile.ArchiveFileData.Select(x => (long)x.Size).Sum();
			fsProgress.Report();

			zipFile.Extracting += (s, e) =>
			{
				if (fsProgress.TotalSize > 0)
					fsProgress.Report(e.BytesProcessed / (double)fsProgress.TotalSize * 100);
			};
			zipFile.FileExtractionStarted += (s, e) =>
			{
				if (cancellationToken.IsCancellationRequested)
					e.Cancel = true;
				if (!e.FileInfo.IsDirectory)
				{
					_threadingService.ExecuteOnUiThreadAsync(() =>
					{
						fsProgress.FileName = e.FileInfo.FileName;
						fsProgress.Report();
					});
				}
			};
			zipFile.FileExtractionFinished += (s, e) =>
			{
				if (!e.FileInfo.IsDirectory)
				{
					fsProgress.AddProcessedItemsCount(1);
					fsProgress.Report();
				}
			};

			try
			{
				// TODO: Get this method return result
				await zipFile.ExtractArchiveAsync(destinationFolder.Path);
			}
			catch (Exception ex)
			{
				App.Logger.LogWarning(ex, $"Error extracting file: {archive.Name}");

				return false;
			}

			return true;
		}

		private static async Task DecompressArchiveAsync(BaseStorageFile archive, BaseStorageFolder? destinationFolder, string password)
		{
			if (archive is null || destinationFolder is null)
				return;

			// Initialize a new in-progress status card
			var statusCard = StatusCenterHelper.AddCard_Decompress(
				archive.Path.CreateEnumerable(),
				destinationFolder.Path.CreateEnumerable(),
				ReturnResult.InProgress);

			// Operate decompress
			var result = await FilesystemTasks.Wrap(() =>
				DecompressArchiveAsync(archive, destinationFolder, password, statusCard.ProgressEventSource, statusCard.CancellationToken));

			// Remove the in-progress status card
			_statusCenterViewModel.RemoveItem(statusCard);

			if (result.Result)
			{
				// Successful
				StatusCenterHelper.AddCard_Decompress(
					archive.Path.CreateEnumerable(),
					destinationFolder.Path.CreateEnumerable(),
					ReturnResult.Success);
			}
			else
			{
				// Error
				StatusCenterHelper.AddCard_Decompress(
					archive.Path.CreateEnumerable(),
					destinationFolder.Path.CreateEnumerable(),
					statusCard.CancellationToken.IsCancellationRequested
						? ReturnResult.Cancelled
						: ReturnResult.Failed);
			}
		}

		public static async Task DecompressArchiveAsync(IShellPage associatedInstance)
		{
			if (associatedInstance == null)
				return;

			BaseStorageFile archive = await StorageHelpers.ToStorageItem<BaseStorageFile>(associatedInstance.SlimContentPage?.SelectedItems?.Count is null or 0
				? associatedInstance.FilesystemViewModel.WorkingDirectory
				: associatedInstance.SlimContentPage.SelectedItem.ItemPath);

			if (archive?.Path is null)
				return;

			var isArchiveEncrypted = await FilesystemTasks.Wrap(() => DecompressHelper.IsArchiveEncrypted(archive));
			var password = string.Empty;

			DecompressArchiveDialog decompressArchiveDialog = new();
			DecompressArchiveDialogViewModel decompressArchiveViewModel = new(archive)
			{
				IsArchiveEncrypted = isArchiveEncrypted,
				ShowPathSelection = true
			};
			decompressArchiveDialog.ViewModel = decompressArchiveViewModel;

			if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
				decompressArchiveDialog.XamlRoot = MainWindow.Instance.Content.XamlRoot;

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

			await DecompressArchiveAsync(archive, destinationFolder, password);

			if (decompressArchiveViewModel.OpenDestinationFolderOnCompletion)
				await NavigationHelpers.OpenPath(destinationFolderPath, associatedInstance, FilesystemItemType.Directory);
		}

		public static async Task DecompressArchiveHereAsync(IShellPage associatedInstance)
		{
			if (associatedInstance?.SlimContentPage?.SelectedItems == null)
				return;

			foreach (var selectedItem in associatedInstance.SlimContentPage.SelectedItems)
			{
				var password = string.Empty;
				BaseStorageFile archive = await StorageHelpers.ToStorageItem<BaseStorageFile>(selectedItem.ItemPath);
				BaseStorageFolder currentFolder = await StorageHelpers.ToStorageItem<BaseStorageFolder>(associatedInstance.FilesystemViewModel.CurrentFolder.ItemPath);

				if (archive?.Path is null)
					return;

				if (await FilesystemTasks.Wrap(() => IsArchiveEncrypted(archive)))
				{
					DecompressArchiveDialog decompressArchiveDialog = new();
					DecompressArchiveDialogViewModel decompressArchiveViewModel = new(archive)
					{
						IsArchiveEncrypted = true,
						ShowPathSelection = false
					};

					decompressArchiveDialog.ViewModel = decompressArchiveViewModel;

					if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
						decompressArchiveDialog.XamlRoot = MainWindow.Instance.Content.XamlRoot;

					ContentDialogResult option = await decompressArchiveDialog.TryShowAsync();
					if (option != ContentDialogResult.Primary)
						return;

					password = Encoding.UTF8.GetString(decompressArchiveViewModel.Password);
				}

				await DecompressArchiveAsync(archive, currentFolder, password);
			}
		}

		public static async Task DecompressArchiveToChildFolderAsync(IShellPage associatedInstance)
		{
			if (associatedInstance?.SlimContentPage?.SelectedItems == null)
				return;

			foreach (var selectedItem in associatedInstance.SlimContentPage.SelectedItems)
			{
				var password = string.Empty;

				BaseStorageFile archive = await StorageHelpers.ToStorageItem<BaseStorageFile>(selectedItem.ItemPath);
				BaseStorageFolder currentFolder = await StorageHelpers.ToStorageItem<BaseStorageFolder>(associatedInstance.FilesystemViewModel.CurrentFolder.ItemPath);
				BaseStorageFolder destinationFolder = null;

				if (archive?.Path is null)
					return;

				if (await FilesystemTasks.Wrap(() => DecompressHelper.IsArchiveEncrypted(archive)))
				{
					DecompressArchiveDialog decompressArchiveDialog = new();
					DecompressArchiveDialogViewModel decompressArchiveViewModel = new(archive)
					{
						IsArchiveEncrypted = true,
						ShowPathSelection = false
					};
					decompressArchiveDialog.ViewModel = decompressArchiveViewModel;

					if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
						decompressArchiveDialog.XamlRoot = MainWindow.Instance.Content.XamlRoot;

					ContentDialogResult option = await decompressArchiveDialog.TryShowAsync();
					if (option != ContentDialogResult.Primary)
						return;

					password = Encoding.UTF8.GetString(decompressArchiveViewModel.Password);
				}

				if (currentFolder is not null)
					destinationFolder = await FilesystemTasks.Wrap(() => currentFolder.CreateFolderAsync(Path.GetFileNameWithoutExtension(archive.Path), CreationCollisionOption.GenerateUniqueName).AsTask());

				await DecompressArchiveAsync(archive, destinationFolder, password);
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

		private static async Task<bool> IsArchiveEncrypted(BaseStorageFile archive)
		{
			using SevenZipExtractor? zipFile = await GetZipFile(archive);
			if (zipFile is null)
				return true;

			return zipFile.ArchiveFileData.Any(file => file.Encrypted || file.Method.Contains("Crypto") || file.Method.Contains("AES"));
		}
	}
}
