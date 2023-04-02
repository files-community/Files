using ByteSizeLib;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.Filesystem.StorageItems;
using Files.App.Helpers;
using Files.App.Shell;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

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
				ViewModel.ItemPath = (Item as RecycleBinItem)?.ItemOriginalFolder ??
					(Path.IsPathRooted(Item.ItemPath) ? Path.GetDirectoryName(Item.ItemPath) : Item.ItemPath);
				ViewModel.ItemModifiedTimestamp = Item.ItemDateModified;
				ViewModel.ItemCreatedTimestamp = Item.ItemDateCreated;
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
						await App.Window.DispatcherQueue.EnqueueAsync(
							() => NavigationHelpers.OpenPathInNewTab(Path.GetDirectoryName(Environment.ExpandEnvironmentVariables(ViewModel.ShortcutItemPath))));
					},
					() =>
					{
						return !string.IsNullOrWhiteSpace(ViewModel.ShortcutItemPath);
					});
				}
			}
		}

		public async override void GetSpecialProperties()
		{
			ViewModel.IsHidden = NativeFileOperationsHelper.HasFileAttribute(
				Item.ItemPath, System.IO.FileAttributes.Hidden);

			var fileIconData = await FileThumbnailHelper.LoadIconFromPathAsync(Item.ItemPath, 80, Windows.Storage.FileProperties.ThumbnailMode.SingleItem, true);
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
				ViewModel.ItemCreatedTimestamp = Item.ItemDateCreated;
				ViewModel.ItemAccessedTimestamp = Item.ItemDateAccessed;
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
				ViewModel.ItemCreatedTimestamp = dateTimeFormatter.ToShortLabel(storageFolder.DateCreated);
				if (storageFolder.Properties is not null)
				{
					GetOtherProperties(storageFolder.Properties);
				}
				GetFolderSize(storageFolder.Path, TokenSource.Token);
			}
			else if (Item.ItemPath.Equals(CommonPaths.RecycleBinPath, StringComparison.OrdinalIgnoreCase))
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
				GetFolderSize(folderPath, TokenSource.Token);
			}
		}

		private async void GetFolderSize(string folderPath, CancellationToken token)
		{
			if (string.IsNullOrEmpty(folderPath))
			{
				// In MTP devices calculating folder size would be too slow
				// Also should use StorageFolder methods instead of FindFirstFileExFromApp
				return;
			}

			ViewModel.ItemSizeVisibility = true;
			ViewModel.ItemSizeProgressVisibility = true;

			var fileSizeTask = Task.Run(async () =>
			{
				var size = await CalculateFolderSizeAsync(folderPath, token);
				return size;
			});

			try
			{
				var folderSize = await fileSizeTask;
				ViewModel.ItemSizeBytes = folderSize;
				ViewModel.ItemSize = folderSize.ToLongSizeString();
			}
			catch (Exception ex)
			{
				App.Logger.LogWarning(ex, ex.Message);
			}

			ViewModel.ItemSizeProgressVisibility = false;

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
