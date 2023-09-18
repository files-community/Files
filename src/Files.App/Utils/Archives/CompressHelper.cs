// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.IO;

namespace Files.App.Utils.Archives
{
	/// <summary>
	/// Provides static helper for compressing archive.
	/// </summary>
	public static class CompressHelper
	{
		private readonly static StatusCenterViewModel _statusCenterViewModel = Ioc.Default.GetRequiredService<StatusCenterViewModel>();

		public static bool CanCompress(IReadOnlyList<ListedItem> selectedItems)
		{
			return !DecompressHelper.CanDecompress(selectedItems) || selectedItems.Count > 1;
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

			// Add in-progress status banner
			var banner = StatusCenterHelper.PostBanner_Compress(
				creator.Sources,
				archivePath.CreateEnumerable(),
				ReturnResult.InProgress,
				false,
				0);

			creator.Progress = banner.ProgressEventSource;

			// Perform compress operation
			bool isSuccess = await creator.RunCreationAsync();

			// Remove in-progress status banner
			_statusCenterViewModel.RemoveItem(banner);

			if (isSuccess)
			{
				// Add successful status banner
				StatusCenterHelper.PostBanner_Compress(
					creator.Sources,
					archivePath.CreateEnumerable(),
					ReturnResult.Success,
					false,
					creator.Sources.Count());
			}
			else
			{
				NativeFileOperationsHelper.DeleteFileFromApp(archivePath);

				// Add error status banner
				StatusCenterHelper.PostBanner_Compress(
					creator.Sources,
					archivePath.CreateEnumerable(),
					ReturnResult.Failed,
					false,
					0);
			}
		}
	}
}
