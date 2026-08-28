// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using System.IO;
using ByteSize = ByteSizeLib.ByteSize;

namespace Files.App.ViewModels.Properties
{
	internal sealed class FolderProperties : BaseProperties
	{
		private readonly IStorageTrashBinService StorageTrashBinService = Ioc.Default.GetRequiredService<IStorageTrashBinService>();

		public ListedItem Item { get; }

		public FolderProperties(
			SelectedItemsPropertiesViewModel viewModel,
			CancellationTokenSource tokenSource,
			DispatcherQueue coreDispatcher,
			ListedItem item,
			IShellPage instance)
			: base(viewModel, tokenSource, coreDispatcher, instance)
		{
			Item = item;

			GetBaseProperties();

			ViewModel.PropertyChanged += ViewModel_PropertyChanged;
		}

		public override void GetBaseProperties()
		{
			var itemPath = Item.GetRequiredPath();
			ViewModel.ItemName = Item.Name;
			ViewModel.OriginalItemName = Item.Name;
			ViewModel.ItemType = Item.ItemType;
			ViewModel.ItemLocation = (Item as RecycleBinItem)?.ItemOriginalFolder ??
				(Path.IsPathRooted(itemPath) ? Path.GetDirectoryName(itemPath) : itemPath);
			ViewModel.ItemModifiedTimestampReal = Item.ItemDateModifiedReal;
			ViewModel.ItemCreatedTimestampReal = Item.ItemDateCreatedReal;
			ViewModel.LoadCustomIcon = Item.LoadCustomIcon;
			ViewModel.CustomIconSource = Item.CustomIconSource;
			ViewModel.LoadFileIcon = Item.LoadFileIcon;
			ViewModel.ContainsFilesOrFolders = Item.ContainsFilesOrFolders;

			if (Item.IsShortcut && Item is IShortcutItem shortcutItem)
			{
				ViewModel.ShortcutItemType = Strings.Folder.GetLocalizedResource();
				ViewModel.ShortcutItemPath = shortcutItem.TargetPath;
				ViewModel.IsShortcutItemPathReadOnly = false;
				ViewModel.ShortcutItemWorkingDir = shortcutItem.WorkingDirectory;
				ViewModel.ShowWindowCommand = shortcutItem.ShowWindowCommand;
				ViewModel.ShortcutItemWorkingDirVisibility = false;
				ViewModel.ShortcutItemArguments = shortcutItem.Arguments;
				ViewModel.ShortcutItemArgumentsVisibility = false;
				ViewModel.ShortcutItemWindowArgsVisibility = false;
				ViewModel.ShortcutItemOpenLinkCommand = new RelayCommand(async () =>
				{
					var shortcutPath = ViewModel.ShortcutItemPath
						?? throw new InvalidOperationException("The shortcut does not have a target path.");
					await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(
						() => NavigationHelpers.OpenPathInNewTab(Path.GetDirectoryName(Environment.ExpandEnvironmentVariables(shortcutPath)), true));
				},
				() =>
				{
					return !string.IsNullOrWhiteSpace(ViewModel.ShortcutItemPath);
				});
			}
		}

		public async override Task GetSpecialPropertiesAsync()
		{
			var itemPath = Item.GetRequiredPath();
			var fileAttributes = Win32Helper.GetFileAttributes(itemPath);
			ViewModel.IsHidden = fileAttributes.HasFlag(FileAttributes.Hidden);
			ViewModel.CanCompressContent = Win32Helper.CanCompressContent(itemPath);
			ViewModel.IsContentCompressed = fileAttributes.HasFlag(FileAttributes.Compressed);

			var result = await FileThumbnailHelper.GetIconAsync(
				itemPath,
				Constants.ShellIconSizes.ExtraLarge,
				true,
				IconOptions.None);

			if (result is not null)
			{
				ViewModel.IconData = result;
				ViewModel.LoadFolderGlyph = false;
				ViewModel.LoadFileIcon = true;
			}

			if (Item.IsShortcut)
			{
				ViewModel.ItemSizeVisibility = true;
				ViewModel.ItemSize = Item.FileSizeBytes.ToLongSizeString();

				// Only load the size for items on the device
				if (Item.SyncStatusUI.SyncStatus is not CloudDriveSyncStatus.FileOnline and not CloudDriveSyncStatus.FolderOnline)
					ViewModel.ItemSizeOnDisk = Win32Helper.GetFileSizeOnDisk(itemPath)?.ToLongSizeString() ??
					   string.Empty;

				ViewModel.ItemCreatedTimestampReal = Item.ItemDateCreatedReal;
				ViewModel.ItemAccessedTimestampReal = Item.ItemDateAccessedReal;
				if (Item.IsLinkItem || string.IsNullOrWhiteSpace(((IShortcutItem)Item).TargetPath))
				{
					// Can't show any other property
					return;
				}
			}

			var targetPath = (Item as IShortcutItem)?.TargetPath;
			string folderPath = !string.IsNullOrEmpty(targetPath) ? targetPath : Item.GetRequiredPath();
			var shellViewModel = AppInstance.GetRequiredShellViewModel();

			var storageFolder = (await shellViewModel.GetFolderFromPathAsync(folderPath)).Result;

			if (storageFolder is not null)
			{
				ViewModel.ItemCreatedTimestampReal = storageFolder.DateCreated;
				if (storageFolder.Properties is not null)
					_ = GetOtherPropertiesAsync(storageFolder.Properties);

				// Only load the size for items on the device
				if (Item.SyncStatusUI.SyncStatus is not CloudDriveSyncStatus.FileOnline and not
					CloudDriveSyncStatus.FolderOnline and not
					CloudDriveSyncStatus.FolderOfflinePartial)
					_ = GetFolderSizeAsync(storageFolder.Path, TokenSource.Token);
			}
			else if (string.Equals(Item.ItemPath, Constants.UserEnvironmentPaths.RecycleBinPath, StringComparison.OrdinalIgnoreCase))
			{
				var recycleBinQuery = StorageTrashBinService.QueryRecycleBin();
				if (recycleBinQuery.BinSize is long binSize)
				{
					ViewModel.ItemSizeBytes = binSize;
					ViewModel.ItemSize = ByteSize.FromBytes(binSize).ToString();
					ViewModel.ItemSizeVisibility = true;
				}
				else
				{
					ViewModel.ItemSizeVisibility = false;
				}
				ViewModel.ItemSizeOnDisk = string.Empty;
				if (recycleBinQuery.NumItems is long numItems)
				{
					ViewModel.FilesCount = (int)numItems;
					SetItemsCountString();
					ViewModel.FilesAndFoldersCountVisibility = true;
				}
				else
				{
					ViewModel.FilesAndFoldersCountVisibility = false;
				}

				ViewModel.ItemCreatedTimestampVisibility = false;
				ViewModel.ItemAccessedTimestampVisibility = false;
				ViewModel.ItemModifiedTimestampVisibility = false;
				ViewModel.LastSeparatorVisibility = false;
			}
			else
			{
				_ = GetFolderSizeAsync(folderPath, TokenSource.Token);
			}
		}

		private async Task GetFolderSizeAsync(string folderPath, CancellationToken token)
		{
			if (string.IsNullOrEmpty(folderPath))
			{
				// In MTP devices calculating folder size would be too slow
				// Also should use StorageFolder methods instead of FindFirstFileExFromApp
				return;
			}

			ViewModel.ItemSizeVisibility = true;
			ViewModel.ItemSizeProgressVisibility = true;
			ViewModel.ItemSizeOnDiskProgressVisibility = true;

			var fileSizeTask = Task.Run(async () =>
			{
				var size = await CalculateFolderSizeAsync(folderPath, token);
				return size;
			});

			try
			{
				var folderSize = await fileSizeTask;
				ViewModel.ItemSizeBytes = folderSize.size;
				ViewModel.ItemSize = folderSize.size.ToLongSizeString();
				ViewModel.ItemSizeOnDiskBytes = folderSize.sizeOnDisk;
				ViewModel.ItemSizeOnDisk = folderSize.sizeOnDisk.ToLongSizeString();
			}
			catch (Exception ex)
			{
				App.Logger.LogWarning(ex, ex.Message);
			}

			ViewModel.ItemSizeProgressVisibility = false;
			ViewModel.ItemSizeOnDiskProgressVisibility = false;

			SetItemsCountString();
		}

		private async void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			var itemPath = Item.GetRequiredPath();
			switch (e.PropertyName)
			{
				case nameof(ViewModel.IsHidden):
					if (ViewModel.IsHidden is not null)
					{
						if ((bool)ViewModel.IsHidden)
							Win32Helper.SetFileAttribute(itemPath, System.IO.FileAttributes.Hidden);
						else
							Win32Helper.UnsetFileAttribute(itemPath, System.IO.FileAttributes.Hidden);
					}
					break;

				case nameof(ViewModel.IsContentCompressed):
					Win32Helper.SetCompressionAttributeIoctl(itemPath, ViewModel.IsContentCompressed ?? false);
					break;

				case nameof(ViewModel.ShortcutItemPath):
				case nameof(ViewModel.ShortcutItemWorkingDir):
				case nameof(ViewModel.ShowWindowCommand):
				case nameof(ViewModel.ShortcutItemArguments):
					var shortcutItem = (IShortcutItem)Item;

					if (string.IsNullOrWhiteSpace(ViewModel.ShortcutItemPath))
						return;

					await FileOperationsHelpers.CreateOrUpdateLinkAsync(itemPath, ViewModel.ShortcutItemPath, ViewModel.ShortcutItemArguments, ViewModel.ShortcutItemWorkingDir, shortcutItem.RunAsAdmin, ViewModel.ShowWindowCommand);
					break;
			}
		}
	}
}
