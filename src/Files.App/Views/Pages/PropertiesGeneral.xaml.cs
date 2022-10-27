using CommunityToolkit.WinUI;
using Files.App.Filesystem;
using Files.App.Helpers;
using Files.App.Shell;
using Files.App.ViewModels.Properties;
using Files.Shared;
using Files.Shared.Enums;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Storage;

namespace Files.App.Views
{
	public sealed partial class PropertiesGeneral : PropertiesTab
	{
		private readonly Regex letterRegex = new(@"\s*\(\w:\)$");

		public PropertiesGeneral() => InitializeComponent();

		public override async Task<bool> SaveChangesAsync(ListedItem item)
		{
			ViewModel.ItemName = ItemFileName.Text; // Make sure Name is updated

			string newName = ViewModel.ItemName;
			string oldName = ViewModel.OriginalItemName;
			bool hasNewName = !string.IsNullOrWhiteSpace(newName) && newName != oldName;

			var fsVM = AppInstance.FilesystemViewModel;
			var itemMM = AppInstance?.SlimContentPage?.ItemManipulationModel;

			if (BaseProperties is DriveProperties driveProps)
			{
				if (!hasNewName || fsVM is null)
					return false;

				var drive = driveProps.Drive;
				newName = letterRegex.Replace(newName, string.Empty); // Remove "(C:)" from the new label

				Win32API.SetVolumeLabel(drive.Path, newName);
				_ = App.Window.DispatcherQueue.EnqueueAsync(async () =>
				{
					await drive.UpdateLabelAsync();
					await fsVM.SetWorkingDirectoryAsync(drive.Path);
				});
				return true;
			}

			if (BaseProperties is LibraryProperties libProps)
			{
				if (!hasNewName || fsVM is null || !App.LibraryManager.CanCreateLibrary(newName).result)
					return false;

				string libraryPath = libProps.Library.ItemPath;
				newName = $"{newName}{ShellLibraryItem.EXTENSION}";

				var file = new StorageFileWithPath(null, libraryPath);
				var renamed = await AppInstance!.FilesystemHelpers.RenameAsync(file, newName, NameCollisionOption.FailIfExists, false);
				if (renamed is ReturnResult.Success)
				{
					var newPath = Path.Combine(Path.GetDirectoryName(libraryPath)!, newName);
					_ = App.Window.DispatcherQueue.EnqueueAsync(async () =>
					{
						await fsVM.SetWorkingDirectoryAsync(newPath);
					});
					return true;
				}

				return false;
			}

			if (BaseProperties is CombinedProperties combinedProps)
			{
				// Handle the visibility attribute for multiple files
				if (itemMM is not null) // null on homepage
				{
					foreach (var fileOrFolder in combinedProps.List)
					{
						await App.Window.DispatcherQueue.EnqueueAsync(() =>
							UIFilesystemHelpers.SetHiddenAttributeItem(fileOrFolder, ViewModel.IsHidden, itemMM)
						);
					}
				}
				return true;
			}

			// Handle the visibility attribute for a single file
			if (itemMM is not null) // null on homepage
			{
				await App.Window.DispatcherQueue.EnqueueAsync(() =>
					UIFilesystemHelpers.SetHiddenAttributeItem(item, ViewModel.IsHidden, itemMM)
				);
			}

			if (!hasNewName)
				return true;

			return await App.Window.DispatcherQueue.EnqueueAsync(() =>
				UIFilesystemHelpers.RenameFileItemAsync(item, ViewModel.ItemName, AppInstance)
			);
		}

		public override void Dispose()
		{
		}

		private void ItemFileName_GettingFocus(UIElement _, GettingFocusEventArgs e)
		{
			ItemFileName.Text = letterRegex.Replace(ItemFileName.Text, string.Empty);
		}
		private void ItemFileName_LosingFocus(UIElement _, LosingFocusEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(ItemFileName.Text)) 
			{
				ItemFileName.Text = ViewModel.ItemName;
				return;
			}

			var match = letterRegex.Match(ViewModel.OriginalItemName);
			if (match.Success)
				ItemFileName.Text += match.Value;
		}

		private void DiskCleanupButton_Click(object sender, RoutedEventArgs e)
		{
			if (BaseProperties is DriveProperties driveProps)
				StorageSenseHelper.OpenStorageSense(driveProps.Drive.Path);
		}
	}
}
