// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Shared.Helpers;
using System.IO;
using Windows.Storage;

namespace Files.App.Utils.Archives
{
	/// <summary>
	/// Provides static helper for compressing archive.
	/// </summary>
	public static class CompressHelper
	{
		private readonly static StatusCenterViewModel _statusCenterViewModel = Ioc.Default.GetRequiredService<StatusCenterViewModel>();

		public static bool CanDecompress(IReadOnlyList<ListedItem> selectedItems)
		{
			return selectedItems.Any() &&
				(selectedItems.All(x => x.IsArchive)
				|| selectedItems.All(x => x.PrimaryItemAttribute == StorageItemTypes.File && FileExtensionHelpers.IsZipFile(x.FileExtension)));
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

		public static async Task CompressArchiveAsync(ICompressArchiveModel creator)
		{
			var archivePath = creator.GetArchivePath();

			int index = 1;

			while (File.Exists(archivePath) || Directory.Exists(archivePath))
				archivePath = creator.GetArchivePath($" ({++index})");

			creator.ArchivePath = archivePath;

			var banner = StatusCenterHelper.AddCard_Compress(
				creator.Sources,
				archivePath.CreateEnumerable(),
				ReturnResult.InProgress,
				creator.Sources.Count());

			creator.Progress = banner.ProgressEventSource;
			creator.CancellationToken = banner.CancellationToken;

			bool isSuccess = await creator.RunCreationAsync();

			_statusCenterViewModel.RemoveItem(banner);

			if (isSuccess)
			{
				StatusCenterHelper.AddCard_Compress(
					creator.Sources,
					archivePath.CreateEnumerable(),
					ReturnResult.Success,
					creator.Sources.Count());
			}
			else
			{
				NativeFileOperationsHelper.DeleteFileFromApp(archivePath);

				StatusCenterHelper.AddCard_Compress(
					creator.Sources,
					archivePath.CreateEnumerable(),
					creator.CancellationToken.IsCancellationRequested
						? ReturnResult.Cancelled
						: ReturnResult.Failed,
					creator.Sources.Count());
			}
		}
	}
}
