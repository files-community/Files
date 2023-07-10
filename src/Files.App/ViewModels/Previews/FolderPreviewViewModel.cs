// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Utils.StorageItems;
using Files.App.ViewModels.Properties;
using Files.Shared.Services.DateTimeFormatter;
using Microsoft.UI.Xaml.Media.Imaging;
using System.IO;
using Windows.Storage.FileProperties;

namespace Files.App.ViewModels.Previews
{
	public class FolderPreviewViewModel
	{
		private readonly IGeneralSettingsService generalSettingsService = Ioc.Default.GetService<IGeneralSettingsService>();

		private static readonly IDateTimeFormatter dateTimeFormatter = Ioc.Default.GetService<IDateTimeFormatter>();

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

			var iconData = await FileThumbnailHelper.LoadIconFromStorageItemAsync(Folder, 256, ThumbnailMode.SingleItem);
			iconData ??= await FileThumbnailHelper.LoadIconWithoutOverlayAsync(Item.ItemPath, 256, true);

			if (iconData is not null)
				Thumbnail = await iconData.ToBitmapAsync();

			var info = await Folder.GetBasicPropertiesAsync();

			Item.FileDetails = new()
			{
				GetFileProperty("PropertyItemCount", items.Count),
				GetFileProperty("PropertyDateModified", dateTimeFormatter.ToLongLabel(info.DateModified)),
				GetFileProperty("PropertyDateCreated", dateTimeFormatter.ToLongLabel(info.ItemDate)),
				GetFileProperty("PropertyParsingPath", Folder.Path),
			};

			if (GitHelpers.IsRepositoryEx(Item.ItemPath, out var repoPath) &&
				!string.IsNullOrEmpty(repoPath))
			{
				var gitDirectory = GitHelpers.GetGitRepositoryPath(Folder.Path, Path.GetPathRoot(Folder.Path));
				var branches = GitHelpers.GetBranchesNames(gitDirectory);
				var repositoryName = GitHelpers.GetOriginRepositoryName(gitDirectory);

				if(!string.IsNullOrEmpty(gitDirectory))
					Item.FileDetails.Add(GetFileProperty("GitOriginRepositoryName", repositoryName));

				if (branches.Length > 0)
					Item.FileDetails.Add(GetFileProperty("GitCurrentBranch", branches.First().Name));
			}

			var tags = Item.FileTagsUI is not null ? string.Join(',', Item.FileTagsUI.Select(x => x.Name)) : null;
			if (tags is not null)
				Item.FileDetails.Add(GetFileProperty("FileTags", tags));
		}

		private static FileProperty GetFileProperty(string nameResource, object value)
			=> new() { NameResource = nameResource, Value = value };
	}
}
