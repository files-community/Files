// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.ViewModels.Properties;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Windows.Storage;
using Windows.Win32;
using WinRT;

namespace Files.App.Views.Properties
{
	public sealed partial class GeneralPage : BasePropertiesPage
	{
		private readonly DispatcherQueueTimer _updateDateDisplayTimer;
		public GeneralPage()
		{
			InitializeComponent();

			_updateDateDisplayTimer = DispatcherQueue.CreateTimer();
			_updateDateDisplayTimer.Interval = TimeSpan.FromSeconds(1);
			_updateDateDisplayTimer.Tick += UpdateDateDisplayTimer_Tick;
			_updateDateDisplayTimer.Start();
		}

		[DynamicWindowsRuntimeCast(typeof(UIElement))]
		private void EditAlbumCoverButton_PointerEntered(object sender, PointerRoutedEventArgs e)
			=> ((UIElement)sender).Opacity = 1;

		[DynamicWindowsRuntimeCast(typeof(UIElement))]
		private void EditAlbumCoverButton_PointerExited(object sender, PointerRoutedEventArgs e)
			=> ((UIElement)sender).Opacity = 0;

		private void ItemFileName_GettingFocus(UIElement _, GettingFocusEventArgs e)
		{
			if (GetDriveLetterToken() is { } letterToken)
				ItemFileName.Text = RemoveDriveLetterToken(ItemFileName.Text, letterToken);
		}

		private void ItemFileName_LosingFocus(UIElement _, LosingFocusEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(ItemFileName.Text))
			{
				ItemFileName.Text = ViewModel.OriginalItemName ?? ViewModel.ItemName ?? string.Empty;
				return;
			}

			if (GetDriveLetterToken() is not { } letterToken)
				return;

			var originalItemName = ViewModel.OriginalItemName
				?? throw new InvalidOperationException("The original item name has not been initialized.");

			// Put the drive letter back on the side it came from
			if (originalItemName.StartsWith(letterToken, StringComparison.OrdinalIgnoreCase))
				ItemFileName.Text = $"{letterToken} {ItemFileName.Text}";
			else if (originalItemName.EndsWith(letterToken, StringComparison.OrdinalIgnoreCase))
				ItemFileName.Text = $"{ItemFileName.Text} {letterToken}";
		}

		// The system can show the drive letter before or after the label, e.g. "(C:) Local Disk" or "Local Disk (C:)"
		private string? GetDriveLetterToken()
			=> BaseProperties is DriveProperties properties && properties.Drive.Path is { Length: > 0 } path
				? $"({path.TrimEnd('\\')})"
				: null;

		private static string RemoveDriveLetterToken(string name, string letterToken)
		{
			if (name.StartsWith(letterToken, StringComparison.OrdinalIgnoreCase))
				return name[letterToken.Length..].TrimStart();
			if (name.EndsWith(letterToken, StringComparison.OrdinalIgnoreCase))
				return name[..^letterToken.Length].TrimEnd();

			return name;
		}

		private void UpdateDateDisplayTimer_Tick(object sender, object e)
		{
			if (App.AppModel.PropertiesWindowCount == 0)
				return;

			// Reassign values to update date display
			ViewModel.ItemCreatedTimestampReal = ViewModel.ItemCreatedTimestampReal;
			ViewModel.ItemModifiedTimestampReal = ViewModel.ItemModifiedTimestampReal;
			ViewModel.ItemAccessedTimestampReal = ViewModel.ItemAccessedTimestampReal;
		}

		public override async Task<bool> SaveChangesAsync()
		{
			return BaseProperties switch
			{
				DriveProperties properties => SaveDrive(properties.Drive),
				LibraryProperties properties => await SaveLibraryAsync(properties.Library),
				CombinedProperties properties => await SaveCombinedAsync(properties.List),
				FileProperties properties => await SaveBaseAsync(properties.Item),
				FolderProperties properties => await SaveBaseAsync(properties.Item),
				_ => throw new UnreachableException()
			};

			bool GetNewName([NotNullWhen(true)] out string? newName)
			{
				if (ItemFileName is not null)
				{
					ViewModel.ItemName = ItemFileName.Text; // Make sure Name is updated
					newName = ViewModel.ItemName;
					string? oldName = ViewModel.OriginalItemName;
					return !string.IsNullOrWhiteSpace(newName) && newName != oldName;
				}
				newName = "";
				return false;
			}

			bool SaveDrive(DriveItem drive)
			{
				var fsVM = AppInstance.ShellViewModel;
				if (!GetNewName(out var newName) || fsVM is null)
					return false;

				if (GetDriveLetterToken() is { } letterToken)
					newName = RemoveDriveLetterToken(newName, letterToken); // Remove "(C:)" from the new label

				if (drive.Type == Data.Items.DriveType.Network)
					Win32Helper.SetNetworkDriveLabel(drive.DeviceID
						?? throw new InvalidOperationException("The network drive does not have a device ID."), newName);
				else
					Win32Helper.SetVolumeLabel(drive.GetRequiredPath(), newName);

				ViewModel.OriginalItemName = ViewModel.ItemName;

				var drivePath = drive.Path;
				_ = MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(async () =>
				{
					if (string.IsNullOrEmpty(drivePath))
						return;

					// Reload the root since the cached one still reports the old label
					var rootModified = await FilesystemTasks.Wrap(() => StorageFolder.GetFolderFromPathAsync(drivePath).AsTask());
					if (rootModified)
					{
						drive.Root = rootModified.Result!;
						drive.Text = rootModified.Result!.DisplayName;
					}

					// Refresh the path display only when this instance is browsing the renamed drive
					var workingDirectory = fsVM.WorkingDirectory;
					if (Path.IsPathRooted(workingDirectory) &&
						string.Equals(Path.GetPathRoot(workingDirectory), Path.GetPathRoot(drivePath), StringComparison.OrdinalIgnoreCase))
						await fsVM.SetWorkingDirectoryAsync(workingDirectory);
				});
				return true;
			}

			async Task<bool> SaveLibraryAsync(LibraryItem library)
			{
				var fsVM = AppInstance.ShellViewModel;
				if (!GetNewName(out var newName) || fsVM is null || !App.LibraryManager.CanCreateLibrary(newName).result)
					return false;

				newName = $"{newName}{ShellLibraryItem.EXTENSION}";

				var libraryPath = library.GetRequiredPath();
				var file = new StorageFileWithPath(null, libraryPath);
				var renamed = await AppInstance.FilesystemHelpers.RenameAsync(file, newName, NameCollisionOption.FailIfExists, false, false);
				if (renamed is ReturnResult.Success)
				{
					var libraryDirectory = Path.GetDirectoryName(libraryPath)
						?? throw new InvalidOperationException("The library path does not have a parent directory.");
					var newPath = Path.Combine(libraryDirectory, newName);
					_ = MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(async () =>
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
					ViewModel.IsContentCompressed = ViewModel.IsContentCompressedEditedValue;

					foreach (var fileOrFolder in fileOrFolders)
					{
						if (ViewModel.IsHiddenEditedValue is not null)
						{
							var isHiddenEditedValue = (bool)ViewModel.IsHiddenEditedValue;
							await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(() =>
								UIFilesystemHelpers.SetHiddenAttributeItem(fileOrFolder, isHiddenEditedValue, itemMM)
							);
							ViewModel.IsHidden = isHiddenEditedValue;
						}

						ViewModel.IsReadOnly = ViewModel.IsReadOnlyEditedValue;

						if (ViewModel.IsAblumCoverModified)
						{
							MediaFileHelper.ChangeAlbumCover(fileOrFolder.GetRequiredPath(), ViewModel.ModifiedAlbumCover);

							await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(() =>
							{
								AppInstance?.ShellViewModel?.RefreshItems(null);
							});
						}
					}
				}
				return true;
			}

			async Task<bool> SaveBaseAsync(ListedItem item)
			{
				var itemPath = item.GetRequiredPath();
				// Handle the visibility attribute for a single file
				var itemMM = AppInstance?.SlimContentPage?.ItemManipulationModel;
				if (itemMM is not null && ViewModel.IsHiddenEditedValue is not null) // null on homepage
				{
					await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(() =>
						UIFilesystemHelpers.SetHiddenAttributeItem(item, (bool)ViewModel.IsHiddenEditedValue, itemMM)
					);
				}

				if (ViewModel.IsUnblockFileSelected)
					PInvoke.DeleteFileFromApp($"{itemPath}:Zone.Identifier");

				if (ViewModel.IsAblumCoverModified)
				{
					MediaFileHelper.ChangeAlbumCover(itemPath, ViewModel.ModifiedAlbumCover);

					await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(() =>
					{
						AppInstance?.ShellViewModel?.RefreshItems(null);
					});
				}

				ViewModel.IsReadOnly = ViewModel.IsReadOnlyEditedValue;
				ViewModel.IsHidden = ViewModel.IsHiddenEditedValue;
				ViewModel.IsContentCompressed = ViewModel.IsContentCompressedEditedValue;

				if (!GetNewName(out var newName))
					return true;

				var appInstance = AppInstance!;
				return await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(() =>
					UIFilesystemHelpers.RenameFileItemAsync(item, newName, appInstance, false)
				);
			}
		}

		public override void Dispose()
		{
			_updateDateDisplayTimer.Stop();
		}
	}
}
