// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.ViewModels.Properties;
using Microsoft.UI.Xaml.Media.Imaging;
using System.IO;
using Windows.Storage.FileProperties;

namespace Files.App.ViewModels.Previews
{
	public class FolderPreviewViewModel
	{
		private static readonly IDateTimeFormatter dateTimeFormatter = Ioc.Default.GetRequiredService<IDateTimeFormatter>();

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
			var items = await Folder.GetItemsAsync();

			var iconData = await FileThumbnailHelper.LoadIconFromStorageItemAsync(Folder, 256, ThumbnailMode.SingleItem, ThumbnailOptions.ReturnOnlyIfCached);
			iconData ??= await FileThumbnailHelper.LoadIconWithoutOverlayAsync(Item.ItemPath, 256, true);

			if (iconData is not null)
				Thumbnail = await iconData.ToBitmapAsync();

			var info = await Folder.GetBasicPropertiesAsync();

			Item.FileDetails = new()
			{
				GetFileProperty("PropertyItemCount", items.Count),
				GetFileProperty("PropertyDateModified", info.DateModified),
				GetFileProperty("PropertyDateCreated", info.DateCreated),
				GetFileProperty("PropertyParsingPath", Folder.Path),
			};

			if (GitHelpers.IsRepositoryEx(Item.ItemPath, out var repoPath) &&
				!string.IsNullOrEmpty(repoPath))
			{
				var gitDirectory = GitHelpers.GetGitRepositoryPath(Folder.Path, Path.GetPathRoot(Folder.Path));
				var headName = (await GitHelpers.GetRepositoryHead(gitDirectory))?.Name ?? string.Empty;
				var repositoryName = GitHelpers.GetOriginRepositoryName(gitDirectory);

				if(!string.IsNullOrEmpty(gitDirectory))
					Item.FileDetails.Add(GetFileProperty("GitOriginRepositoryName", repositoryName));

				if (!string.IsNullOrWhiteSpace(headName))
					Item.FileDetails.Add(GetFileProperty("GitCurrentBranch", headName));
			}
		}

		private static FileProperty GetFileProperty(string nameResource, object value)
			=> new() { NameResource = nameResource, Value = value };
	}
}
