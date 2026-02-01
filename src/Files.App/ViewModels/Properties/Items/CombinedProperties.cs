using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using System.IO;
using Windows.Storage;

namespace Files.App.ViewModels.Properties
{
	internal class CombinedProperties : BaseProperties
	{
		public List<ListedItem> List { get; }

		public CombinedProperties(
			SelectedItemsPropertiesViewModel viewModel,
			CancellationTokenSource tokenSource,
			DispatcherQueue coreDispatcher,
			List<ListedItem> listedItems,
			IShellPage instance)
		{
			ViewModel = viewModel;
			TokenSource = tokenSource;
			Dispatcher = coreDispatcher;
			List = listedItems;
			AppInstance = instance;
			GetBaseProperties();
			ViewModel.PropertyChanged += ViewModel_PropertyChanged;
		}

		public sealed override void GetBaseProperties()
		{
			if (List is not null)
			{
				ViewModel.LoadCombinedItemsGlyph = true;

				if (List.All(x => x.ItemType.Equals(List.First().ItemType)))
				{
					ViewModel.ItemType = string.Format(Strings.PropertiesDriveItemTypesEquals.GetLocalizedResource(), List.First().ItemType);
				}
				else
				{
					ViewModel.ItemType = Strings.PropertiesDriveItemTypeDifferent.GetLocalizedResource();
				}

				var itemsPath = List.Select(Item => (Item as RecycleBinItem)?.ItemOriginalFolder ??
					(Path.IsPathRooted(Item.ItemPath) ? Path.GetDirectoryName(Item.ItemPath) : Item.ItemPath));

				if (itemsPath.Distinct().Count() == 1)
					ViewModel.ItemLocation = string.Format(Strings.PropertiesCombinedItemLocation.GetLocalizedResource(), itemsPath.First());
			}
		}

		public override async Task GetSpecialPropertiesAsync()
		{
			bool allFiles = true, allReadOnly = true, allNotReadOnly = true, allHidden = true, allNotHidden = true;
			foreach (var x in List)
			{
				allFiles &= x.PrimaryItemAttribute == StorageItemTypes.File;
				var fileAttributes = Win32Helper.GetFileAttributes(x.ItemPath);
				bool isReadOnly = fileAttributes.HasFlag(System.IO.FileAttributes.ReadOnly);
				allReadOnly &= isReadOnly;
				allNotReadOnly &= !isReadOnly;
				bool isHidden = fileAttributes.HasFlag(System.IO.FileAttributes.Hidden);
				allHidden &= isHidden;
				allNotHidden &= !isHidden;
			}
			
			if (allFiles)
			{
				if (allReadOnly)
					ViewModel.IsReadOnly = true;
				else if (allNotReadOnly)
					ViewModel.IsReadOnly = false;
				else
					ViewModel.IsReadOnly = null;
			}

			if (allHidden)
				ViewModel.IsHidden = true;
			else if (allNotHidden)
				ViewModel.IsHidden = false;
			else
				ViewModel.IsHidden = null;

			ViewModel.LastSeparatorVisibility = false;
			ViewModel.ItemSizeVisibility = true;

			ViewModel.FilesCount += List.Count(x => x.PrimaryItemAttribute == StorageItemTypes.File || x.IsArchive);
			ViewModel.FoldersCount += List.Count(x => x.PrimaryItemAttribute == StorageItemTypes.Folder && !x.IsArchive);

			long filesSize = List.Where(x => x.PrimaryItemAttribute == StorageItemTypes.File).Sum(x => x.FileSizeBytes);
			long foldersSize = 0;
			long filesSizeOnDisk = List.Where(x => x.PrimaryItemAttribute == StorageItemTypes.File &&
				x.SyncStatusUI.SyncStatus is not CloudDriveSyncStatus.FileOnline and not CloudDriveSyncStatus.FolderOnline)
					.Sum(x => Win32Helper.GetFileSizeOnDisk(x.ItemPath) ?? 0);
			long foldersSizeOnDisk = 0;

			ViewModel.ItemSizeProgressVisibility = true;
			ViewModel.ItemSizeOnDiskProgressVisibility = true;

			foreach (var item in List)
			{
				if (item.PrimaryItemAttribute == StorageItemTypes.Folder)
				{
					if (item.SyncStatusUI.SyncStatus is CloudDriveSyncStatus.FileOnline or
						CloudDriveSyncStatus.FolderOnline or
						CloudDriveSyncStatus.FolderOfflinePartial)
						continue;

					var folderSizeTask = Task.Run(() => CalculateFolderSizeAsync(item.ItemPath, TokenSource.Token));

					try
					{
						var folderSize = await folderSizeTask;
						foldersSize += folderSize.size;
						foldersSizeOnDisk += folderSize.sizeOnDisk;
					}
					catch (Exception ex)
					{
						App.Logger.LogWarning(ex, ex.Message);
					}
				}
			}

			ViewModel.ItemSizeProgressVisibility = false;
			ViewModel.ItemSizeOnDiskProgressVisibility = false;

			long totalSize = filesSize + foldersSize;
			ViewModel.ItemSize = totalSize.ToLongSizeString();
			long totalSizeOnDisk = filesSizeOnDisk + foldersSizeOnDisk;
			ViewModel.ItemSizeOnDisk = totalSizeOnDisk.ToLongSizeString();

			SetItemsCountString();
		}

		private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case "IsReadOnly":
					{
						if (ViewModel.IsReadOnly is not null)
						{
							if ((bool)ViewModel.IsReadOnly)
							{
								List.ForEach(x => Win32Helper.SetFileAttribute(
									x.ItemPath, System.IO.FileAttributes.ReadOnly));
							}
							else
							{
								List.ForEach(x => Win32Helper.UnsetFileAttribute(
									x.ItemPath, System.IO.FileAttributes.ReadOnly));
							}
						}
					}
					break;

				case "IsHidden":
					{
						if (ViewModel.IsHidden is not null)
						{
							if ((bool)ViewModel.IsHidden)
							{
								List.ForEach(x => Win32Helper.SetFileAttribute(
									x.ItemPath, System.IO.FileAttributes.Hidden));
							}
							else
							{
								List.ForEach(x => Win32Helper.UnsetFileAttribute(
									x.ItemPath, System.IO.FileAttributes.Hidden));
							}
						}

					}
					break;

				case "IsContentCompressed":
					{
						List.ForEach(x =>
						{
							Win32Helper.SetCompressionAttributeIoctl(x.ItemPath, ViewModel.IsContentCompressed ?? false);
						});
					}
					break;
			}
		}
	}
}
