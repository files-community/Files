// Copyright(c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Shared.Helpers;
using Microsoft.UI.Dispatching;
using System.IO;

namespace Files.App.ViewModels.Properties
{
	public class FileProperties : BaseProperties, IFileProperties
	{
		public ListedItem Item { get; }

		public FileProperties(
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
			if (Item is null)
				return;

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
			ViewModel.IsDownloadedFile = NativeFileOperationsHelper.ReadStringFromFile($"{Item.ItemPath}:Zone.Identifier") is not null;

			if (!Item.IsShortcut)
				return;

			var shortcutItem = (ShortcutItem)Item;

			var isApplication =
				FileExtensionHelpers.IsExecutableFile(shortcutItem.TargetPath) ||
				FileExtensionHelpers.IsMsiFile(shortcutItem.TargetPath);

			ViewModel.ShortcutItemType = isApplication ? "Application".GetLocalizedResource() :
				Item.IsLinkItem ? "PropertiesShortcutTypeLink".GetLocalizedResource() : "File".GetLocalizedResource();
			ViewModel.ShortcutItemPath = shortcutItem.TargetPath;
			ViewModel.IsShortcutItemPathReadOnly = shortcutItem.IsSymLink;
			ViewModel.ShortcutItemWorkingDir = shortcutItem.WorkingDirectory;
			ViewModel.ShortcutItemWorkingDirVisibility = Item.IsLinkItem || shortcutItem.IsSymLink ? false : true;
			ViewModel.ShortcutItemArguments = shortcutItem.Arguments;
			ViewModel.ShortcutItemArgumentsVisibility = Item.IsLinkItem || shortcutItem.IsSymLink ? false : true;

			if (isApplication)
				ViewModel.RunAsAdmin = shortcutItem.RunAsAdmin;

			ViewModel.IsSelectedItemShortcut = FileExtensionHelpers.IsShortcutFile(Item.FileExtension);

			ViewModel.ShortcutItemOpenLinkCommand = new RelayCommand(async () =>
			{
				if (Item.IsLinkItem)
				{
					var tmpItem = (ShortcutItem)Item;
					await Win32Helpers.InvokeWin32ComponentAsync(ViewModel.ShortcutItemPath, AppInstance, ViewModel.ShortcutItemArguments, ViewModel.RunAsAdmin, ViewModel.ShortcutItemWorkingDir);
				}
				else
				{
					await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(
						() => NavigationHelpers.OpenPathInNewTab(Path.GetDirectoryName(ViewModel.ShortcutItemPath)));
				}
			},
			() =>
			{
				return !string.IsNullOrWhiteSpace(ViewModel.ShortcutItemPath);
			});
		}

		public override async Task GetSpecialPropertiesAsync()
		{
			ViewModel.IsReadOnly = NativeFileOperationsHelper.HasFileAttribute(
				Item.ItemPath, System.IO.FileAttributes.ReadOnly);
			ViewModel.IsHidden = NativeFileOperationsHelper.HasFileAttribute(
				Item.ItemPath, System.IO.FileAttributes.Hidden);

			ViewModel.ItemSizeVisibility = true;
			ViewModel.ItemSize = Item.FileSizeBytes.ToLongSizeString();

			// Only load the size for items on the device
			if (Item.SyncStatusUI.SyncStatus is not CloudDriveSyncStatus.FileOnline and not CloudDriveSyncStatus.FolderOnline)
				ViewModel.ItemSizeOnDisk = NativeFileOperationsHelper.GetFileSizeOnDisk(Item.ItemPath)?.ToLongSizeString() ??
				   string.Empty;

			var fileIconData = await FileThumbnailHelper.LoadIconFromPathAsync(Item.ItemPath, 80, Windows.Storage.FileProperties.ThumbnailMode.DocumentsView, Windows.Storage.FileProperties.ThumbnailOptions.ResizeThumbnail, false);
			if (fileIconData is not null)
			{
				ViewModel.IconData = fileIconData;
				ViewModel.LoadUnknownTypeGlyph = false;
				ViewModel.LoadFileIcon = true;
			}

			if (Item.IsShortcut)
			{
				ViewModel.ItemCreatedTimestampReal = Item.ItemDateCreatedReal;
				ViewModel.ItemAccessedTimestampReal = Item.ItemDateAccessedReal;
				ViewModel.LoadLinkIcon = Item.LoadWebShortcutGlyph;
				if (Item.IsLinkItem || string.IsNullOrWhiteSpace(((ShortcutItem)Item).TargetPath))
				{
					// Can't show any other property
					return;
				}
			}

			string filePath = (Item as ShortcutItem)?.TargetPath ?? Item.ItemPath;
			BaseStorageFile file = await AppInstance.FilesystemViewModel.GetFileFromPathAsync(filePath);

			// Couldn't access the file and can't load any other properties
			if (file is null)
				return;

			// Can't load any other properties
			if (Item.IsShortcut)
				return;

			if (Item.SyncStatusUI.SyncStatus is not CloudDriveSyncStatus.FileOnline and not CloudDriveSyncStatus.FolderOnline)
				if (FileExtensionHelpers.IsBrowsableZipFile(Item.FileExtension, out _))
					if (await ZipStorageFolder.FromPathAsync(Item.ItemPath) is ZipStorageFolder zipFolder)
					{
						var uncompressedSize = await zipFolder.GetUncompressedSize();
						ViewModel.UncompressedItemSize = uncompressedSize.ToLongSizeString();
						ViewModel.UncompressedItemSizeBytes = uncompressedSize;
					}

			if (file.Properties is not null)
				GetOtherPropertiesAsync(file.Properties);
		}

		public async Task GetSystemFilePropertiesAsync()
		{
			BaseStorageFile file = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFileFromPathAsync(Item.ItemPath));
			if (file is null)
			{
				// Could not access file, can't show any other property
				return;
			}

			var list = await FileProperty.RetrieveAndInitializePropertiesAsync(file);

			list.Find(x => x.ID == "address").Value =
				await LocationHelpers.GetAddressFromCoordinatesAsync((double?)list.Find(
					x => x.Property == "System.GPS.LatitudeDecimal").Value,
					(double?)list.Find(x => x.Property == "System.GPS.LongitudeDecimal").Value);

			// Find Encoding Bitrate property and convert it to kbps
			var encodingBitrate = list.Find(x => x.Property == "System.Audio.EncodingBitrate");
			if (encodingBitrate?.Value is not null)
			{
				var sizes = new string[] { "Bps", "KBps", "MBps", "GBps" };
				var order = Math.Min((int)Math.Floor(Math.Log((uint)encodingBitrate.Value, 1024)), 3);
				var readableSpeed = (uint)encodingBitrate.Value / Math.Pow(1024, order);
				encodingBitrate.Value = $"{readableSpeed:0.##} {sizes[order]}";
			}

			var query = list
				.Where(fileProp => !(fileProp.Value is null && fileProp.IsReadOnly))
				.GroupBy(fileProp => fileProp.SectionResource)
				.Select(group => new FilePropertySection(group) { Key = group.Key })
				.Where(section => !section.All(fileProp => fileProp.Value is null))
				.OrderBy(group => group.Priority);

			ViewModel.PropertySections = new ObservableCollection<FilePropertySection>(query);
			ViewModel.FileProperties = new ObservableCollection<FileProperty>(list.Where(i => i.Value is not null));
		}

		public async Task SyncPropertyChangesAsync()
		{
			BaseStorageFile file = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFileFromPathAsync(Item.ItemPath));

			// Couldn't access the file to save properties
			if (file is null)
				return;

			var failedProperties = "";

			foreach (var group in ViewModel.PropertySections)
			{
				foreach (FileProperty prop in group)
				{
					if (!prop.IsReadOnly && prop.Modified)
					{
						var newDict = new Dictionary<string, object>();
						newDict.Add(prop.Property, prop.Value);

						try
						{
							if (file.Properties is not null)
							{
								await file.Properties.SavePropertiesAsync(newDict);
							}
						}
						catch
						{
							failedProperties += $"{prop.Name}\n";
						}
					}
				}
			}

			if (!string.IsNullOrWhiteSpace(failedProperties))
			{
				throw new Exception($"The following properties failed to save: {failedProperties}");
			}
		}

		public async Task ClearPropertiesAsync()
		{
			var failedProperties = new List<string>();
			BaseStorageFile file = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFileFromPathAsync(Item.ItemPath));

			if (file is null)
				return;

			foreach (var group in ViewModel.PropertySections)
			{
				foreach (FileProperty prop in group)
				{
					if (!prop.IsReadOnly)
					{
						var newDict = new Dictionary<string, object>();
						newDict.Add(prop.Property, null);

						try
						{
							if (file.Properties is not null)
							{
								await file.Properties.SavePropertiesAsync(newDict);
							}
						}
						catch
						{
							failedProperties.Add(prop.Name);
						}
					}
				}
			}

			_ = GetSystemFilePropertiesAsync();
		}

		private async void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case "IsReadOnly":
					if (ViewModel.IsReadOnly)
					{
						NativeFileOperationsHelper.SetFileAttribute(
							Item.ItemPath,
							System.IO.FileAttributes.ReadOnly
						);
					}
					else
					{
						NativeFileOperationsHelper.UnsetFileAttribute(
							Item.ItemPath,
							System.IO.FileAttributes.ReadOnly
						);
					}

					break;

				case "IsHidden":
					if (ViewModel.IsHidden)
					{
						NativeFileOperationsHelper.SetFileAttribute(
							Item.ItemPath,
							System.IO.FileAttributes.Hidden
						);
					}
					else
					{
						NativeFileOperationsHelper.UnsetFileAttribute(
							Item.ItemPath,
							System.IO.FileAttributes.Hidden
						);
					}

					break;

				case "RunAsAdmin":
				case "ShortcutItemPath":
				case "ShortcutItemWorkingDir":
				case "ShortcutItemArguments":
					if (string.IsNullOrWhiteSpace(ViewModel.ShortcutItemPath))
						return;

					await FileOperationsHelpers.CreateOrUpdateLinkAsync(Item.ItemPath, ViewModel.ShortcutItemPath, ViewModel.ShortcutItemArguments, ViewModel.ShortcutItemWorkingDir, ViewModel.RunAsAdmin);

					break;
			}
		}
	}
}
