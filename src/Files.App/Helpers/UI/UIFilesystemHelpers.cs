// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Dialogs;
using Files.App.Storage.FtpStorage;
using Files.App.ViewModels.Dialogs;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Text;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.System;

namespace Files.App.Helpers
{
	public static class UIFilesystemHelpers
	{
		private static readonly StatusCenterViewModel _statusCenterViewModel = Ioc.Default.GetRequiredService<StatusCenterViewModel>();

		public static async Task CutItemAsync(IShellPage associatedInstance)
		{
			var dataPackage = new DataPackage()
			{
				RequestedOperation = DataPackageOperation.Move
			};
			ConcurrentBag<IStorageItem> items = new();

			if (associatedInstance.SlimContentPage.IsItemSelected)
			{
				// First, reset DataGrid Rows that may be in "cut" command mode
				associatedInstance.SlimContentPage.ItemManipulationModel.RefreshItemsOpacity();

				var itemsCount = associatedInstance.SlimContentPage.SelectedItems!.Count;

				var banner = itemsCount > 50 ? StatusCenterHelper.AddCard_Prepare() : null;

				try
				{
					var dispatcherQueue = DispatcherQueue.GetForCurrentThread();
					if (banner is not null)
					{
						banner.Progress.EnumerationCompleted = true;
						banner.Progress.ItemsCount = items.Count;
						banner.Progress.ReportStatus(FileSystemStatusCode.InProgress);
					}

					await associatedInstance.SlimContentPage.SelectedItems.ToList().ParallelForEachAsync(async listedItem =>
					{
						if (banner is not null)
						{
							banner.Progress.AddProcessedItemsCount(1);
							banner.Progress.Report();
						}

						// FTP don't support cut, fallback to copy
						if (listedItem is not FtpItem)
						{
							_ = dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
							{
								// Dim opacities accordingly
								listedItem.Opacity = Constants.UI.DimItemOpacity;
							});
						}
						if (listedItem is FtpItem ftpItem)
						{
							if (ftpItem.PrimaryItemAttribute is StorageItemTypes.File or StorageItemTypes.Folder)
								items.Add(await ftpItem.ToStorageItem());
						}
						else if (listedItem.PrimaryItemAttribute == StorageItemTypes.File || listedItem is ZipItem)
						{
							var result = await associatedInstance.FilesystemViewModel.GetFileFromPathAsync(listedItem.ItemPath)
								.OnSuccess(t => items.Add(t));

							if (!result)
								throw new IOException($"Failed to process {listedItem.ItemPath}.", (int)result.ErrorCode);
						}
						else
						{
							var result = await associatedInstance.FilesystemViewModel.GetFolderFromPathAsync(listedItem.ItemPath)
								.OnSuccess(t => items.Add(t));

							if (!result)
								throw new IOException($"Failed to process {listedItem.ItemPath}.", (int)result.ErrorCode);
						}
					}, 10, banner?.CancellationToken ?? default);
				}
				catch (Exception ex)
				{
					if (ex.HResult == (int)FileSystemStatusCode.Unauthorized)
					{
						string[] filePaths = associatedInstance.SlimContentPage.SelectedItems.Select(x => x.ItemPath).ToArray();

						await FileOperationsHelpers.SetClipboard(filePaths, DataPackageOperation.Move);

						_statusCenterViewModel.RemoveItem(banner);

						return;
					}

					associatedInstance.SlimContentPage.ItemManipulationModel.RefreshItemsOpacity();

					_statusCenterViewModel.RemoveItem(banner);

					return;
				}

				_statusCenterViewModel.RemoveItem(banner);
			}

			var onlyStandard = items.All(x => x is StorageFile || x is StorageFolder || x is SystemStorageFile || x is SystemStorageFolder);
			if (onlyStandard)
				items = new ConcurrentBag<IStorageItem>(await items.ToStandardStorageItemsAsync());

			if (!items.Any())
				return;

			dataPackage.Properties.PackageFamilyName = Windows.ApplicationModel.Package.Current.Id.FamilyName;
			dataPackage.SetStorageItems(items, false);
			try
			{
				Clipboard.SetContent(dataPackage);
			}
			catch
			{
				dataPackage = null;
			}
		}

		public static async Task CopyItemAsync(IShellPage associatedInstance)
		{
			var dataPackage = new DataPackage()
			{
				RequestedOperation = DataPackageOperation.Copy
			};
			ConcurrentBag<IStorageItem> items = new();

			if (associatedInstance.SlimContentPage.IsItemSelected)
			{
				associatedInstance.SlimContentPage.ItemManipulationModel.RefreshItemsOpacity();

				var itemsCount = associatedInstance.SlimContentPage.SelectedItems!.Count;

				var banner = itemsCount > 50 ? StatusCenterHelper.AddCard_Prepare() : null;

				try
				{
					if (banner is not null)
					{
						banner.Progress.EnumerationCompleted = true;
						banner.Progress.ItemsCount = items.Count;
						banner.Progress.ReportStatus(FileSystemStatusCode.InProgress);
					}
					await associatedInstance.SlimContentPage.SelectedItems.ToList().ParallelForEachAsync(async listedItem =>
					{
						if (banner is not null)
						{
							banner.Progress.AddProcessedItemsCount(1);
							banner.Progress.Report();
						}

						if (listedItem is FtpItem ftpItem)
						{
							if (ftpItem.PrimaryItemAttribute is StorageItemTypes.File or StorageItemTypes.Folder)
								items.Add(await ftpItem.ToStorageItem());
						}
						else if (listedItem.PrimaryItemAttribute == StorageItemTypes.File || listedItem is ZipItem)
						{
							var result = await associatedInstance.FilesystemViewModel.GetFileFromPathAsync(listedItem.ItemPath)
								.OnSuccess(t => items.Add(t));

							if (!result)
								throw new IOException($"Failed to process {listedItem.ItemPath}.", (int)result.ErrorCode);
						}
						else
						{
							var result = await associatedInstance.FilesystemViewModel.GetFolderFromPathAsync(listedItem.ItemPath)
								.OnSuccess(t => items.Add(t));

							if (!result)
								throw new IOException($"Failed to process {listedItem.ItemPath}.", (int)result.ErrorCode);
						}
					}, 10, banner?.CancellationToken ?? default);
				}
				catch (Exception ex)
				{
					if (ex.HResult == (int)FileSystemStatusCode.Unauthorized)
					{
						string[] filePaths = associatedInstance.SlimContentPage.SelectedItems.Select(x => x.ItemPath).ToArray();

						await FileOperationsHelpers.SetClipboard(filePaths, DataPackageOperation.Copy);

						_statusCenterViewModel.RemoveItem(banner);

						return;
					}

					_statusCenterViewModel.RemoveItem(banner);

					return;
				}

				_statusCenterViewModel.RemoveItem(banner);
			}

			var onlyStandard = items.All(x => x is StorageFile || x is StorageFolder || x is SystemStorageFile || x is SystemStorageFolder);
			if (onlyStandard)
				items = new ConcurrentBag<IStorageItem>(await items.ToStandardStorageItemsAsync());

			if (!items.Any())
				return;

			dataPackage.Properties.PackageFamilyName = Windows.ApplicationModel.Package.Current.Id.FamilyName;
			dataPackage.SetStorageItems(items, false);

			try
			{
				Clipboard.SetContent(dataPackage);
			}
			catch
			{
				dataPackage = null;
			}
		}

		public static async Task PasteItemAsync(string destinationPath, IShellPage associatedInstance)
		{
			FilesystemResult<DataPackageView> packageView = await FilesystemTasks.Wrap(() => Task.FromResult(Clipboard.GetContent()));
			if (packageView && packageView.Result is not null)
			{
				await associatedInstance.FilesystemHelpers.PerformOperationTypeAsync(packageView.Result.RequestedOperation, packageView, destinationPath, false, true);
				associatedInstance.SlimContentPage?.ItemManipulationModel?.RefreshItemsOpacity();
				await associatedInstance.RefreshIfNoWatcherExistsAsync();
			}
		}

		public static async Task<bool> RenameFileItemAsync(ListedItem item, string newName, IShellPage associatedInstance, bool showExtensionDialog = true)
		{
			if (item is AlternateStreamItem ads) // For alternate streams Name is not a substring ItemNameRaw
			{
				newName = item.ItemNameRaw.Replace(
					item.Name.Substring(item.Name.LastIndexOf(':') + 1),
					newName.Substring(newName.LastIndexOf(':') + 1),
					StringComparison.Ordinal);
				newName = $"{ads.MainStreamName}:{newName}";
			}
			else if (string.IsNullOrEmpty(item.Name))
			{
				newName = string.Concat(newName, item.FileExtension);
			}
			else
			{
				newName = item.ItemNameRaw.Replace(item.Name, newName, StringComparison.Ordinal);
			}

			if (item.ItemNameRaw == newName || string.IsNullOrEmpty(newName))
				return true;

			FilesystemItemType itemType = (item.PrimaryItemAttribute == StorageItemTypes.Folder) ? FilesystemItemType.Directory : FilesystemItemType.File;

			ReturnResult renamed = await associatedInstance.FilesystemHelpers.RenameAsync(StorageHelpers.FromPathAndType(item.ItemPath, itemType), newName, NameCollisionOption.FailIfExists, true, showExtensionDialog);

			if (renamed == ReturnResult.Success)
			{
				associatedInstance.ToolbarViewModel.CanGoForward = false;
				await associatedInstance.RefreshIfNoWatcherExistsAsync();
				return true;
			}

			return false;
		}

		public static async Task CreateFileFromDialogResultTypeAsync(AddItemDialogItemType itemType, ShellNewEntry? itemInfo, IShellPage associatedInstance)
		{
			await CreateFileFromDialogResultTypeForResult(itemType, itemInfo, associatedInstance);
			await associatedInstance.RefreshIfNoWatcherExistsAsync();
		}

		private static async Task<IStorageItem?> CreateFileFromDialogResultTypeForResult(AddItemDialogItemType itemType, ShellNewEntry? itemInfo, IShellPage associatedInstance)
		{
			string? currentPath = null;

			if (associatedInstance.SlimContentPage is not null)
			{
				currentPath = associatedInstance.FilesystemViewModel.WorkingDirectory;
				if (App.LibraryManager.TryGetLibrary(currentPath, out var library) &&
					!library.IsEmpty &&
					library.Folders.Count == 1) // TODO: handle libraries with multiple folders
				{
					currentPath = library.Folders.First();
				}
			}

			// Skip rename dialog when ShellNewEntry has a Command (e.g. ".accdb", ".gdoc")
			string? userInput = null;
			if (itemType != AddItemDialogItemType.File || itemInfo?.Command is null)
			{
				DynamicDialog dialog = DynamicDialogFactory.GetFor_RenameDialog();
				await dialog.TryShowAsync(); // Show rename dialog

				if (dialog.DynamicResult != DynamicDialogResult.Primary)
					return null;

				userInput = dialog.ViewModel.AdditionalData as string;
			}

			// Create file based on dialog result
			(ReturnResult Status, IStorageItem Item) created = (ReturnResult.Failed, null);
			switch (itemType)
			{
				case AddItemDialogItemType.Folder:
					userInput = !string.IsNullOrWhiteSpace(userInput) ? userInput : "NewFolder".GetLocalizedResource();
					created = await associatedInstance.FilesystemHelpers.CreateAsync(
						StorageHelpers.FromPathAndType(PathNormalization.Combine(currentPath, userInput), FilesystemItemType.Directory),
						true);
					break;

				case AddItemDialogItemType.File:
					userInput = !string.IsNullOrWhiteSpace(userInput) ? userInput : itemInfo?.Name ?? "NewFile".GetLocalizedResource();
					created = await associatedInstance.FilesystemHelpers.CreateAsync(
						StorageHelpers.FromPathAndType(PathNormalization.Combine(currentPath, userInput + itemInfo?.Extension), FilesystemItemType.File),
						true);
					break;
			}

			// Add newly created item to recent files list
			if (created.Status == ReturnResult.Success && created.Item?.Path is not null)
				App.RecentItemsManager.AddToRecentItems(created.Item.Path);
			else if (created.Status == ReturnResult.AccessUnauthorized)
			{
				await DialogDisplayHelper.ShowDialogAsync
				(
					"AccessDenied".GetLocalizedResource(),
					"AccessDeniedCreateDialog/Text".GetLocalizedResource()
				);
			}

			return created.Item;
		}

		public static async Task CreateFolderWithSelectionAsync(IShellPage associatedInstance)
		{
			try
			{
				var items = associatedInstance.SlimContentPage.SelectedItems.ToList().Select((item) => StorageHelpers.FromPathAndType(
					item.ItemPath,
					item.PrimaryItemAttribute == StorageItemTypes.File ? FilesystemItemType.File : FilesystemItemType.Directory));
				var folder = await CreateFileFromDialogResultTypeForResult(AddItemDialogItemType.Folder, null, associatedInstance);
				if (folder is null)
					return;

				await associatedInstance.FilesystemHelpers.MoveItemsAsync(items, items.Select(x => PathNormalization.Combine(folder.Path, x.Name)), false, true);
				await associatedInstance.RefreshIfNoWatcherExistsAsync();
			}
			catch (Exception ex)
			{
				App.Logger.LogWarning(ex, null);
			}
		}

		/// <summary>
		/// Set a single file or folder to hidden or unhidden and refresh the
		/// view after setting the flag
		/// </summary>
		/// <param name="item"></param>
		/// <param name="isHidden"></param>
		public static void SetHiddenAttributeItem(ListedItem item, bool isHidden, ItemManipulationModel itemManipulationModel)
		{
			item.IsHiddenItem = isHidden;
			itemManipulationModel.RefreshItemsOpacity();
		}

		public static async Task CreateShortcutAsync(IShellPage? associatedInstance, IReadOnlyList<ListedItem> selectedItems)
		{
			var currentPath = associatedInstance?.FilesystemViewModel.WorkingDirectory;

			if (App.LibraryManager.TryGetLibrary(currentPath ?? string.Empty, out var library) && !library.IsEmpty)
				currentPath = library.DefaultSaveFolder;

			foreach (ListedItem selectedItem in selectedItems)
			{
				var fileName = FilesystemHelpers.GetShortcutNamingPreference(selectedItem.Name);
				var filePath = Path.Combine(currentPath ?? string.Empty, fileName);

				if (!await FileOperationsHelpers.CreateOrUpdateLinkAsync(filePath, selectedItem.ItemPath))
					await HandleShortcutCannotBeCreated(fileName, selectedItem.ItemPath);
			}

			if (associatedInstance is not null)
				await associatedInstance.RefreshIfNoWatcherExistsAsync();
		}

		public static async Task CreateShortcutFromDialogAsync(IShellPage associatedInstance)
		{
			var currentPath = associatedInstance.FilesystemViewModel.WorkingDirectory;
			if (App.LibraryManager.TryGetLibrary(currentPath, out var library) &&
				!library.IsEmpty)
			{
				currentPath = library.DefaultSaveFolder;
			}

			var viewModel = new CreateShortcutDialogViewModel(currentPath);
			var dialogService = Ioc.Default.GetRequiredService<IDialogService>();
			var result = await dialogService.ShowDialogAsync(viewModel);

			if (result != DialogResult.Primary || viewModel.ShortcutCreatedSuccessfully)
				return;

			await HandleShortcutCannotBeCreated(viewModel.ShortcutCompleteName, viewModel.DestinationItemPath);

			await associatedInstance.RefreshIfNoWatcherExistsAsync();
		}

		public static async Task<bool> HandleShortcutCannotBeCreated(string shortcutName, string destinationPath)
		{
			var result = await DialogDisplayHelper.ShowDialogAsync
			(
				"CannotCreateShortcutDialogTitle".ToLocalized(),
				"CannotCreateShortcutDialogMessage".ToLocalized(),
				"Create".ToLocalized(),
				"Cancel".ToLocalized()
			);
			if (!result)
				return false;

			var shortcutPath = Path.Combine(Constants.UserEnvironmentPaths.DesktopPath, shortcutName);

			return await FileOperationsHelpers.CreateOrUpdateLinkAsync(shortcutPath, destinationPath);
		}

		/// <summary>
		/// Updates ListedItem properties for a shortcut
		/// </summary>
		/// <param name="item"></param>
		/// <param name="targetPath"></param>
		/// <param name="arguments"></param>
		/// <param name="workingDir"></param>
		/// <param name="runAsAdmin"></param>
		public static void UpdateShortcutItemProperties(ShortcutItem item, string targetPath, string arguments, string workingDir, bool runAsAdmin)
		{
			item.TargetPath = Environment.ExpandEnvironmentVariables(targetPath);
			item.Arguments = arguments;
			item.WorkingDirectory = workingDir;
			item.RunAsAdmin = runAsAdmin;
		}

		public async static Task<StorageCredential> RequestPassword(IPasswordProtectedItem sender)
		{
			var path = ((IStorageItem)sender).Path;
			var isFtp = FtpHelpers.IsFtpPath(path);

			var credentialDialogViewModel = new CredentialDialogViewModel() { CanBeAnonymous = isFtp, PasswordOnly = !isFtp };
			IDialogService dialogService = Ioc.Default.GetRequiredService<IDialogService>();
			var dialogResult = await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(() =>
				dialogService.ShowDialogAsync(credentialDialogViewModel));

			if (dialogResult != DialogResult.Primary || credentialDialogViewModel.IsAnonymous)
				return new();

			// Can't do more than that to mitigate immutability of strings. Perhaps convert DisposableArray to SecureString immediately?
			var credentials = new StorageCredential(credentialDialogViewModel.UserName, Encoding.UTF8.GetString(credentialDialogViewModel.Password));
			credentialDialogViewModel.Password?.Dispose();

			if (isFtp)
			{
				var host = FtpHelpers.GetFtpHost(path);
				FtpManager.Credentials[host] = new NetworkCredential(credentials.UserName, credentials.SecurePassword);
			}

			return credentials;
		}
	}
}