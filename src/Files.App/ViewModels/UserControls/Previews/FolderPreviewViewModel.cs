// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.ViewModels.Properties;
using Microsoft.UI.Xaml.Media.Imaging;
using System.IO;

namespace Files.App.ViewModels.Previews
{
	public sealed class FolderPreviewViewModel
	{
		private readonly IContentPageContext _contentPageContext = Ioc.Default.GetRequiredService<IContentPageContext>();

		public ListedItem Item { get; }

		public BitmapImage Thumbnail { get; set; } = new();

		private BaseStorageFolder Folder { get; set; }

		public FolderPreviewViewModel(ListedItem item)
			=> Item = item;

		public Task LoadAsync()
			=> LoadPreviewAndDetailsAsync();

		private async Task LoadPreviewAndDetailsAsync()
		{
			var rootItem = await FilesystemTasks.Wrap(() => DriveHelpers.GetRootFromPathAsync(Item.ItemPath));
			Folder = await StorageFileExtensions.DangerousGetFolderFromPathAsync(Item.ItemPath, rootItem);

			// Get actual item count including hidden files based on user settings
			int itemCount = await GetItemCountAsync();

			var result = await FileThumbnailHelper.GetIconAsync(
				Item.ItemPath,
				Constants.ShellIconSizes.Jumbo,
				true,
				IconOptions.None);

			if (result is not null)
				Thumbnail = await result.ToBitmapAsync();

			// If the selected item is the root of a drive (e.g. "C:\")
			// we do not need to load the properties below, since they will not be shown.
			// Drive properties will be obtained through the DrivesViewModel service.
			if (Item.IsDriveRoot)
				return;

			var info = await Folder.GetBasicPropertiesAsync();

			Item.FileDetails =
			[
				GetFileProperty("PropertyItemCount", itemCount),
				GetFileProperty("PropertyDateModified", info.DateModified),
				GetFileProperty("PropertyDateCreated", info.DateCreated),
				GetFileProperty("PropertyParsingPath", Folder.Path),
			];

			if (GitHelpers.IsRepositoryEx(Item.ItemPath, out var repoPath) &&
				!string.IsNullOrEmpty(repoPath))
			{
				var gitDirectory = GitHelpers.GetGitRepositoryPath(Folder.Path, Path.GetPathRoot(Folder.Path));
				var headName = (await GitHelpers.GetRepositoryHead(gitDirectory))?.Name ?? string.Empty;
				var repositoryName = GitHelpers.GetOriginRepositoryName(gitDirectory);

				if (!string.IsNullOrEmpty(gitDirectory))
					Item.FileDetails.Add(GetFileProperty("GitOriginRepositoryName", repositoryName));

				if (!string.IsNullOrWhiteSpace(headName))
					Item.FileDetails.Add(GetFileProperty("GitCurrentBranch", headName));
			}
		}

		private async Task<int> GetItemCountAsync()
		{
			// If this is the current folder being viewed, use ShellViewModel's TotalItemCount
			// which includes hidden files when the setting is enabled (same as status bar)
			if (_contentPageContext.ShellPage?.ShellViewModel is not null &&
				_contentPageContext.Folder?.ItemPath == Item.ItemPath)
			{
				return _contentPageContext.ShellPage.ShellViewModel.TotalItemCount;
			}

			// For other folders (e.g., selected folder in the file list), enumerate directly
			// This respects the user's "Show hidden files" setting
			return await Task.Run(() => CountItemsInFolder(Item.ItemPath));
		}

		private static int CountItemsInFolder(string folderPath)
		{
			try
			{
				var userSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();
				var showHiddenItems = userSettingsService.FoldersSettingsService.ShowHiddenItems;
				var showProtectedSystemFiles = userSettingsService.FoldersSettingsService.ShowProtectedSystemFiles;

				int count = 0;
				var directory = new DirectoryInfo(folderPath);

				// Count files
				foreach (var file in directory.EnumerateFiles())
				{
					var isHidden = (file.Attributes & System.IO.FileAttributes.Hidden) != 0;
					var isSystem = (file.Attributes & System.IO.FileAttributes.System) != 0;

					// Skip hidden & system files if their respective settings are off
					if (isHidden && (!showHiddenItems || (isSystem && !showProtectedSystemFiles)))
						continue;

					count++;
				}

				// Count directories
				foreach (var dir in directory.EnumerateDirectories())
				{
					var isHidden = (dir.Attributes & System.IO.FileAttributes.Hidden) != 0;
					var isSystem = (dir.Attributes & System.IO.FileAttributes.System) != 0;

					// Skip hidden & system directories if their respective settings are off
					if (isHidden && (!showHiddenItems || (isSystem && !showProtectedSystemFiles)))
						continue;

					count++;
				}

				return count;
			}
			catch (UnauthorizedAccessException)
			{
				// If we can't access the folder, return 0
				return 0;
			}
			catch (Exception)
			{
				// For any other error, return 0
				return 0;
			}
		}

		private static FileProperty GetFileProperty(string nameResource, object value)
			=> new() { NameResource = nameResource, Value = value };
	}
}
