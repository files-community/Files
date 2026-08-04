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
			: base(viewModel, tokenSource, coreDispatcher, instance)
		{
			List = listedItems;
			GetBaseProperties();
			ViewModel.PropertyChanged += ViewModel_PropertyChanged;
		}

		public sealed override void GetBaseProperties()
		{
			ViewModel.LoadCombinedItemsGlyph = true;
			var firstItemType = List.First().ItemType
				?? throw new InvalidOperationException("A selected item does not have a type.");

			if (List.All(x => string.Equals(
				x.ItemType ?? throw new InvalidOperationException("A selected item does not have a type."),
				firstItemType,
				StringComparison.Ordinal)))
			{
				ViewModel.ItemType = string.Format(Strings.PropertiesDriveItemTypesEquals.GetLocalizedResource(), firstItemType);
			}
			else
			{
				ViewModel.ItemType = Strings.PropertiesDriveItemTypeDifferent.GetLocalizedResource();
			}

			var itemsPath = List.Select(item =>
			{
				var itemPath = item.GetRequiredPath();
				return (item as RecycleBinItem)?.ItemOriginalFolder ??
					(Path.IsPathRooted(itemPath) ? Path.GetDirectoryName(itemPath) : itemPath);
			});

			if (itemsPath.Distinct().Count() == 1)
				ViewModel.ItemLocation = string.Format(Strings.PropertiesCombinedItemLocation.GetLocalizedResource(), itemsPath.First());
		}

		public override async Task GetSpecialPropertiesAsync()
		{
			var itemsWithPaths = List.Select(item => (
				Item: item,
				Path: item.GetRequiredPath())).ToList();
			bool allFiles = true, allReadOnly = true, allNotReadOnly = true, allHidden = true, allNotHidden = true;
			bool allCompressed = true, allNotCompressed = true, anyCanCompress = false;
			foreach (var (x, path) in itemsWithPaths)
			{
				allFiles &= x.PrimaryItemAttribute == StorageItemTypes.File;
				var fileAttributes = Win32Helper.GetFileAttributes(path);
				bool isReadOnly = fileAttributes.HasFlag(System.IO.FileAttributes.ReadOnly);
				allReadOnly &= isReadOnly;
				allNotReadOnly &= !isReadOnly;
				bool isHidden = fileAttributes.HasFlag(System.IO.FileAttributes.Hidden);
				allHidden &= isHidden;
				allNotHidden &= !isHidden;
				bool isCompressed = fileAttributes.HasFlag(System.IO.FileAttributes.Compressed);
				allCompressed &= isCompressed;
				allNotCompressed &= !isCompressed;
				anyCanCompress |= Win32Helper.CanCompressContent(path);
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

			ViewModel.CanCompressContent = anyCanCompress;
			if (allCompressed)
				ViewModel.IsContentCompressed = true;
			else if (allNotCompressed)
				ViewModel.IsContentCompressed = false;
			else
				ViewModel.IsContentCompressed = null;

			ViewModel.LastSeparatorVisibility = false;
			ViewModel.ItemSizeVisibility = true;

			ViewModel.FilesCount += List.Count(x => x.PrimaryItemAttribute == StorageItemTypes.File || x.IsArchive);
			ViewModel.FoldersCount += List.Count(x => x.PrimaryItemAttribute == StorageItemTypes.Folder && !x.IsArchive);

			long filesSize = List.Where(x => x.PrimaryItemAttribute == StorageItemTypes.File).Sum(x => x.FileSizeBytes);
			long foldersSize = 0;
			long filesSizeOnDisk = itemsWithPaths.Where(x => x.Item.PrimaryItemAttribute == StorageItemTypes.File &&
				x.Item.SyncStatusUI.SyncStatus is not CloudDriveSyncStatus.FileOnline and not CloudDriveSyncStatus.FolderOnline)
					.Sum(x => Win32Helper.GetFileSizeOnDisk(x.Path) ?? 0);
			long foldersSizeOnDisk = 0;

			ViewModel.ItemSizeProgressVisibility = true;
			ViewModel.ItemSizeOnDiskProgressVisibility = true;

			foreach (var (item, path) in itemsWithPaths)
			{
				if (item.PrimaryItemAttribute == StorageItemTypes.Folder)
				{
					if (item.SyncStatusUI.SyncStatus is CloudDriveSyncStatus.FileOnline or
						CloudDriveSyncStatus.FolderOnline or
						CloudDriveSyncStatus.FolderOfflinePartial)
						continue;

					var folderSizeTask = Task.Run(() => CalculateFolderSizeAsync(path, TokenSource.Token));

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

		private async void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case "IsReadOnly":
					{
						if (ViewModel.IsReadOnly is not null)
						{
							var itemPaths = List.Select(item => item.GetRequiredPath()).ToList();
							if ((bool)ViewModel.IsReadOnly)
							{
								itemPaths.ForEach(path => Win32Helper.SetFileAttribute(
									path, System.IO.FileAttributes.ReadOnly));
							}
							else
							{
								itemPaths.ForEach(path => Win32Helper.UnsetFileAttribute(
									path, System.IO.FileAttributes.ReadOnly));
							}
						}
					}
					break;

				case "IsHidden":
					{
						if (ViewModel.IsHidden is not null)
						{
							var itemPaths = List.Select(item => item.GetRequiredPath()).ToList();
							if ((bool)ViewModel.IsHidden)
							{
								itemPaths.ForEach(path => Win32Helper.SetFileAttribute(
									path, System.IO.FileAttributes.Hidden));
							}
							else
							{
								itemPaths.ForEach(path => Win32Helper.UnsetFileAttribute(
									path, System.IO.FileAttributes.Hidden));
							}
						}

					}
					break;

				case "IsContentCompressed":
					{
						var isCompressed = ViewModel.IsContentCompressed ?? false;
						var items = List.Select(item => item.GetRequiredPath()).ToList();
						await Task.Run(() =>
						{
							foreach (var path in items)
								Win32Helper.SetCompressionAttributeIoctl(path, isCompressed);
						});
					}
					break;
			}
		}
	}
}
