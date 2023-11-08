// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using System.IO;
using ByteSize = ByteSizeLib.ByteSize;

namespace Files.App.ViewModels.Properties
{
	internal class FolderProperties : BaseProperties
	{
		public ListedItem Item { get; }

		public FolderProperties(
			SelectedItemsPropertiesViewModel viewModel,
			CancellationTokenSource tokenSource,
			DispatcherQueue coreDispatcher,
			ListedItem item,
			IShellPage instance)
		{
			ViewModel = viewModel;
			TokenSource = tokenSource;
			Dispatcher = coreDispatcher;
			Item = item;
			AppInstance = instance;

			GetBaseProperties();

			ViewModel.PropertyChanged += ViewModel_PropertyChanged;
		}

		public override void GetBaseProperties()
		{
			if (Item is not null)
			{
				ViewModel.ItemName = Item.Name;
				ViewModel.OriginalItemName = Item.Name;
				ViewModel.ItemType = Item.ItemType;
				ViewModel.ItemLocation = (Item as RecycleBinItem)?.ItemOriginalFolder ??
					(Path.IsPathRooted(Item.ItemPath) ? Path.GetDirectoryName(Item.ItemPath) : Item.ItemPath);
				ViewModel.ItemModifiedTimestampReal = Item.ItemDateModifiedReal;
				ViewModel.ItemCreatedTimestampReal = Item.ItemDateCreatedReal;
				ViewModel.LoadCustomIcon = Item.LoadCustomIcon;
				ViewModel.CustomIconSource = Item.CustomIconSource;
				ViewModel.LoadFileIcon = Item.LoadFileIcon;
				ViewModel.ContainsFilesOrFolders = Item.ContainsFilesOrFolders;

				if (Item.IsShortcut)
				{
					var shortcutItem = (ShortcutItem)Item;
					ViewModel.ShortcutItemType = "Folder".GetLocalizedResource();
					ViewModel.ShortcutItemPath = shortcutItem.TargetPath;
					ViewModel.IsShortcutItemPathReadOnly = false;
					ViewModel.ShortcutItemWorkingDir = shortcutItem.WorkingDirectory;
					ViewModel.ShortcutItemWorkingDirVisibility = false;
					ViewModel.ShortcutItemArguments = shortcutItem.Arguments;
					ViewModel.ShortcutItemArgumentsVisibility = false;
					ViewModel.ShortcutItemOpenLinkCommand = new RelayCommand(async () =>
					{
						await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(
							() => NavigationHelpers.OpenPathInNewTab(Path.GetDirectoryName(Environment.ExpandEnvironmentVariables(ViewModel.ShortcutItemPath))));
					},
					() =>
					{
						return !string.IsNullOrWhiteSpace(ViewModel.ShortcutItemPath);
					});
				}
			}
		}

		public async override Task GetSpecialPropertiesAsync()
		{
			ViewModel.IsHidden = NativeFileOperationsHelper.HasFileAttribute(
				Item.ItemPath, System.IO.FileAttributes.Hidden);

			var fileIconData = await FileThumbnailHelper.LoadIconFromPathAsync(Item.ItemPath, 80, Windows.Storage.FileProperties.ThumbnailMode.SingleItem, Windows.Storage.FileProperties.ThumbnailOptions.UseCurrentScale, true);
			if (fileIconData is not null)
			{
				ViewModel.IconData = fileIconData;
				ViewModel.LoadFolderGlyph = false;
				ViewModel.LoadFileIcon = true;
			}

			if (Item.IsShortcut)
			{
				ViewModel.ItemSizeVisibility = true;
				ViewModel.ItemSize = Item.FileSizeBytes.ToLongSizeString();

				// Only load the size for items on the device
				if (Item.SyncStatusUI.SyncStatus is not CloudDriveSyncStatus.FileOnline and not CloudDriveSyncStatus.FolderOnline)
					ViewModel.ItemSizeOnDisk = NativeFileOperationsHelper.GetFileSizeOnDisk(Item.ItemPath)?.ToLongSizeString() ??
					   string.Empty;

				ViewModel.ItemCreatedTimestampReal = Item.ItemDateCreatedReal;
				ViewModel.ItemAccessedTimestampReal = Item.ItemDateAccessedReal;
				if (Item.IsLinkItem || string.IsNullOrWhiteSpace(((ShortcutItem)Item).TargetPath))
				{
					// Can't show any other property
					return;
				}
			}

			string folderPath = (Item as ShortcutItem)?.TargetPath ?? Item.ItemPath;
			BaseStorageFolder storageFolder = await AppInstance.FilesystemViewModel.GetFolderFromPathAsync(folderPath);

			if (storageFolder is not null)
			{
				ViewModel.ItemCreatedTimestampReal = storageFolder.DateCreated;
				if (storageFolder.Properties is not null)
					GetOtherPropertiesAsync(storageFolder.Properties);

				// Only load the size for items on the device
				if (Item.SyncStatusUI.SyncStatus is not CloudDriveSyncStatus.FileOnline and not 
					CloudDriveSyncStatus.FolderOnline and not
					CloudDriveSyncStatus.FolderOfflinePartial)
					GetFolderSizeAsync(storageFolder.Path, TokenSource.Token);
			}
			else if (Item.ItemPath.Equals(Constants.UserEnvironmentPaths.RecycleBinPath, StringComparison.OrdinalIgnoreCase))
			{
				var recycleBinQuery = Win32Shell.QueryRecycleBin();
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
				GetFolderSizeAsync(folderPath, TokenSource.Token);
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

		private async void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case "IsHidden":
					if (ViewModel.IsHidden)
					{
						NativeFileOperationsHelper.SetFileAttribute(
							Item.ItemPath, System.IO.FileAttributes.Hidden);
					}
					else
					{
						NativeFileOperationsHelper.UnsetFileAttribute(
							Item.ItemPath, System.IO.FileAttributes.Hidden);
					}
					break;

				case "ShortcutItemPath":
				case "ShortcutItemWorkingDir":
				case "ShortcutItemArguments":
					var tmpItem = (ShortcutItem)Item;

					if (string.IsNullOrWhiteSpace(ViewModel.ShortcutItemPath))
						return;

					await FileOperationsHelpers.CreateOrUpdateLinkAsync(Item.ItemPath, ViewModel.ShortcutItemPath, ViewModel.ShortcutItemArguments, ViewModel.ShortcutItemWorkingDir, tmpItem.RunAsAdmin);
					break;
			}
		}
	}
}
