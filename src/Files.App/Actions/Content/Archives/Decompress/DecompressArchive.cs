// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Dialogs;
using Files.Shared.Helpers;
using Microsoft.UI.Xaml.Controls;
using System.IO;
using System.Text;
using Windows.Foundation.Metadata;
using Windows.Storage;

namespace Files.App.Actions
{
	internal sealed partial class DecompressArchive : BaseDecompressArchiveAction
	{
		public override string Label
			=> Strings.ExtractFiles.GetLocalizedResource();

		public override string Description
			=> Strings.DecompressArchiveDescription.GetLocalizedFormatResource(context.SelectedItems.Count);

		public override HotKey HotKey
			=> new(Keys.E, KeyModifiers.Ctrl);

		public DecompressArchive()
		{
		}

		public override async Task ExecuteAsync(object? parameter = null)
		{
			if (context.ShellPage is null)
				return;

			var archivePath = GetArchivePath();

			if (string.IsNullOrEmpty(archivePath))
				return;

			BaseStorageFile archive = await StorageHelpers.ToStorageItem<BaseStorageFile>(archivePath);

			if (archive?.Path is null)
				return;

			var isArchiveEncrypted = await FilesystemTasks.Wrap(() => StorageArchiveService.IsEncryptedAsync(archive.Path));
			var isArchiveEncodingUndetermined = await FilesystemTasks.Wrap(() => StorageArchiveService.IsEncodingUndeterminedAsync(archive.Path));
			Encoding? detectedEncoding = null;
			if (isArchiveEncodingUndetermined)
			{
				detectedEncoding = await FilesystemTasks.Wrap(() => StorageArchiveService.DetectEncodingAsync(archive.Path));
			}
			var password = string.Empty;
			Encoding? encoding = null;

			DecompressArchiveDialog decompressArchiveDialog = new();
			DecompressArchiveDialogViewModel decompressArchiveViewModel = new(archive)
			{
				IsArchiveEncrypted = isArchiveEncrypted,
				IsArchiveEncodingUndetermined = isArchiveEncodingUndetermined,
				ShowPathSelection = true,
				DetectedEncoding = detectedEncoding,
			};
			decompressArchiveDialog.ViewModel = decompressArchiveViewModel;

			if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
				decompressArchiveDialog.XamlRoot = MainWindow.Instance.Content.XamlRoot;

			ContentDialogResult option = await decompressArchiveDialog.TryShowAsync();
			if (option != ContentDialogResult.Primary)
				return;

			if (isArchiveEncrypted && decompressArchiveViewModel.Password is not null)
				password = Encoding.UTF8.GetString(decompressArchiveViewModel.Password);

			encoding = decompressArchiveViewModel.SelectedEncoding.Encoding;

			// Check if archive still exists
			if (!StorageHelpers.Exists(archive.Path))
				return;

			BaseStorageFolder destinationFolder = decompressArchiveViewModel.DestinationFolder;
			string destinationFolderPath = decompressArchiveViewModel.DestinationFolderPath;

			if (destinationFolder is null)
			{
				BaseStorageFolder parentFolder = await StorageHelpers.ToStorageItem<BaseStorageFolder>(Path.GetDirectoryName(archive.Path) ?? string.Empty);
				destinationFolder = await FilesystemTasks.Wrap(() => parentFolder.CreateFolderAsync(Path.GetFileName(destinationFolderPath), CreationCollisionOption.GenerateUniqueName).AsTask());
			}

			// Operate decompress
			var result = await FilesystemTasks.Wrap(() =>
				StorageArchiveService.DecompressAsync(archive?.Path ?? string.Empty, destinationFolder?.Path ?? string.Empty, password, encoding));

			if (decompressArchiveViewModel.OpenDestinationFolderOnCompletion)
				await NavigationHelpers.OpenPath(destinationFolderPath, context.ShellPage, FilesystemItemType.Directory);
		}

		protected override bool CanDecompressInsideArchive()
		{
			return
				context.PageType == ContentPageTypes.ZipFolder &&
				!context.HasSelection &&
				context.Folder is not null &&
				FileExtensionHelpers.IsZipFile(Path.GetExtension(context.Folder.ItemPath));
		}

		protected override bool CanDecompressSelectedItems()
		{
			return context.SelectedItems.Count == 1 && base.CanDecompressSelectedItems();
		}

		private string? GetArchivePath()
		{
			if (!string.IsNullOrEmpty(context.SelectedItem?.ItemPath))
				return context.SelectedItem?.ItemPath;

			if (context.PageType == ContentPageTypes.ZipFolder && !context.HasSelection)
				return context.Folder?.ItemPath;

			return null;
		}
	}
}
