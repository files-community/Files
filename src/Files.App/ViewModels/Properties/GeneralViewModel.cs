using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using Files.App.DataModels.NavigationControlItems;
using Files.App.Filesystem;
using Files.App.Helpers;
using Files.App.Shell;
using Files.Shared;
using Files.Shared.Enums;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace Files.App.ViewModels.Properties
{
	public class GeneralViewModel : ObservableObject
	{
		public IAsyncRelayCommand OpenDiskCleanupButtonCommand { get; set; }

		public GeneralViewModel()
		{
			OpenDiskCleanupButtonCommand = new AsyncRelayCommand<BaseProperties>(ExecuteOpenDiskCleanupButton);
		}

		private void ExecuteOpenDiskCleanupButton(BaseProperties? baseProperties)
		{
			if (baseProperties is DriveProperties driveProps)
				StorageSenseHelper.OpenStorageSense(driveProps.Drive.Path);
		}

		public async Task<bool> SaveChanges()
		{
			return BaseProperties switch
			{
				DriveProperties properties => SaveDrive(properties.Drive),
				LibraryProperties properties => await SaveLibraryAsync(properties.Library),
				CombinedProperties properties => await SaveCombinedAsync(properties.List),
				FileProperties properties => await SaveBaseAsync(properties.Item),
				FolderProperties properties => await SaveBaseAsync(properties.Item),
			};

			bool GetNewName(out string newName)
			{
				if (ItemFileName is not null)
				{
					ViewModel.ItemName = ItemFileName.Text; // Make sure Name is updated
					newName = ViewModel.ItemName;
					string oldName = ViewModel.OriginalItemName;
					return !string.IsNullOrWhiteSpace(newName) && newName != oldName;
				}
				newName = "";
				return false;
			}

			bool SaveDrive(DriveItem drive)
			{
				var fsVM = AppInstance.FilesystemViewModel;
				if (!GetNewName(out var newName) || fsVM is null)
					return false;

				newName = letterRegex.Replace(newName, string.Empty); // Remove "(C:)" from the new label

				Win32API.SetVolumeLabel(drive.Path, newName);
				_ = App.Window.DispatcherQueue.EnqueueAsync(async () =>
				{
					await drive.UpdateLabelAsync();
					await fsVM.SetWorkingDirectoryAsync(drive.Path);
				});
				return true;
			}

			async Task<bool> SaveLibraryAsync(LibraryItem library)
			{
				var fsVM = AppInstance.FilesystemViewModel;
				if (!GetNewName(out var newName) || fsVM is null || !App.LibraryManager.CanCreateLibrary(newName).result)
					return false;

				newName = $"{newName}{ShellLibraryItem.EXTENSION}";

				var file = new StorageFileWithPath(null, library.ItemPath);
				var renamed = await AppInstance!.FilesystemHelpers.RenameAsync(file, newName, NameCollisionOption.FailIfExists, false, false);
				if (renamed is ReturnResult.Success)
				{
					var newPath = Path.Combine(Path.GetDirectoryName(library.ItemPath)!, newName);
					_ = App.Window.DispatcherQueue.EnqueueAsync(async () =>
					{
						await fsVM.SetWorkingDirectoryAsync(newPath);
					});
					return true;
				}

				return false;
			}

			async Task<bool> SaveCombinedAsync(IList<ListedItem> fileOrFolders)
			{
				// Handle the visibility attribute for multiple files
				var itemMM = AppInstance?.SlimContentPage?.ItemManipulationModel;
				if (itemMM is not null) // null on homepage
				{
					foreach (var fileOrFolder in fileOrFolders)
					{
						await App.Window.DispatcherQueue.EnqueueAsync(() =>
							UIFilesystemHelpers.SetHiddenAttributeItem(fileOrFolder, ViewModel.IsHidden, itemMM)
						);
					}
				}
				return true;
			}

			async Task<bool> SaveBaseAsync(ListedItem item)
			{
				// Handle the visibility attribute for a single file
				var itemMM = AppInstance?.SlimContentPage?.ItemManipulationModel;
				if (itemMM is not null) // null on homepage
				{
					await App.Window.DispatcherQueue.EnqueueAsync(() =>
						UIFilesystemHelpers.SetHiddenAttributeItem(item, ViewModel.IsHidden, itemMM)
					);
				}

				if (!GetNewName(out var newName))
					return true;

				return await App.Window.DispatcherQueue.EnqueueAsync(() =>
					UIFilesystemHelpers.RenameFileItemAsync(item, ViewModel.ItemName, AppInstance, false)
				);
			}
		}
	}
}
