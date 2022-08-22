using Files.Shared;
using Files.Uwp.Dialogs;
using Files.Shared.Enums;
using Files.Shared.Extensions;
using Files.Uwp.Filesystem;
using Files.Uwp.Filesystem.StorageItems;
using Files.Uwp.Interacts;
using Files.Uwp.ViewModels;
using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation.Collections;
using Windows.Storage;
using Files.Backend.Enums;
using Windows.System;

namespace Files.Uwp.Helpers
{
    public static class UIFilesystemHelpers
    {
        public static async void CutItem(IShellPage associatedInstance)
        {
            DataPackage dataPackage = new DataPackage()
            {
                RequestedOperation = DataPackageOperation.Move
            };
            ConcurrentBag<IStorageItem> items = new ConcurrentBag<IStorageItem>();

            if (associatedInstance.SlimContentPage.IsItemSelected)
            {
                // First, reset DataGrid Rows that may be in "cut" command mode
                associatedInstance.SlimContentPage.ItemManipulationModel.RefreshItemsOpacity();

                var itemsCount = associatedInstance.SlimContentPage.SelectedItems.Count;
                PostedStatusBanner banner = itemsCount > 50 ? App.OngoingTasksViewModel.PostOperationBanner(
                    string.Empty,
                    string.Format("StatusPreparingItemsDetails_Plural".GetLocalized(), itemsCount),
                    0,
                    ReturnResult.InProgress,
                    FileOperationType.Prepare, new CancellationTokenSource()) : null;

                try
                {
                    var dispatcherQueue = DispatcherQueue.GetForCurrentThread();
                    await associatedInstance.SlimContentPage.SelectedItems.ToList().ParallelForEachAsync(async listedItem =>
                    {
                        if (banner != null)
                        {
                            ((IProgress<float>)banner.Progress).Report(items.Count / (float)itemsCount * 100);
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
                            {
                                items.Add(await ftpItem.ToStorageItem());
                            }
                        }
                        else if (listedItem.PrimaryItemAttribute == StorageItemTypes.File || listedItem is ZipItem)
                        {
                            var result = await associatedInstance.FilesystemViewModel.GetFileFromPathAsync(listedItem.ItemPath)
                                .OnSuccess(t => items.Add(t));
                            if (!result)
                            {
                                throw new IOException($"Failed to process {listedItem.ItemPath}.", (int)result.ErrorCode);
                            }
                        }
                        else
                        {
                            var result = await associatedInstance.FilesystemViewModel.GetFolderFromPathAsync(listedItem.ItemPath)
                                .OnSuccess(t => items.Add(t));
                            if (!result)
                            {
                                throw new IOException($"Failed to process {listedItem.ItemPath}.", (int)result.ErrorCode);
                            }
                        }
                    }, 10, banner?.CancellationToken ?? default);
                }
                catch (Exception ex)
                {
                    if (ex.HResult == (int)FileSystemStatusCode.Unauthorized)
                    {
                        // Try again with fulltrust process
                        var connection = await AppServiceConnectionHelper.Instance;
                        if (connection != null)
                        {
                            string filePaths = string.Join('|', associatedInstance.SlimContentPage.SelectedItems.Select(x => x.ItemPath));
                            AppServiceResponseStatus status = await connection.SendMessageAsync(new ValueSet()
                            {
                                { "Arguments", "FileOperation" },
                                { "fileop", "Clipboard" },
                                { "filepath", filePaths },
                                { "operation", (int)DataPackageOperation.Move }
                            });
                            if (status == AppServiceResponseStatus.Success)
                            {
                                banner?.Remove();
                                return;
                            }
                        }
                    }
                    associatedInstance.SlimContentPage.ItemManipulationModel.RefreshItemsOpacity();
                    banner?.Remove();
                    return;
                }

                banner?.Remove();
            }

            var onlyStandard = items.All(x => x is StorageFile || x is StorageFolder || x is SystemStorageFile || x is SystemStorageFolder);
            if (onlyStandard)
            {
                items = new ConcurrentBag<IStorageItem>(await items.ToStandardStorageItemsAsync());
            }
            if (!items.Any())
            {
                return;
            }
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

        public static async Task CopyItem(IShellPage associatedInstance)
        {
            DataPackage dataPackage = new DataPackage()
            {
                RequestedOperation = DataPackageOperation.Copy
            };
            ConcurrentBag<IStorageItem> items = new ConcurrentBag<IStorageItem>();

            if (associatedInstance.SlimContentPage.IsItemSelected)
            {
                var itemsCount = associatedInstance.SlimContentPage.SelectedItems.Count;
                PostedStatusBanner banner = itemsCount > 50 ? App.OngoingTasksViewModel.PostOperationBanner(
                    string.Empty,
                    string.Format("StatusPreparingItemsDetails_Plural".GetLocalized(), itemsCount),
                    0,
                    ReturnResult.InProgress,
                    FileOperationType.Prepare, new CancellationTokenSource()) : null;

                try
                {
                    await associatedInstance.SlimContentPage.SelectedItems.ToList().ParallelForEachAsync(async listedItem =>
                    {
                        if (banner != null)
                        {
                            ((IProgress<float>)banner.Progress).Report(items.Count / (float)itemsCount * 100);
                        }

                        if (listedItem is FtpItem ftpItem)
                        {
                            if (ftpItem.PrimaryItemAttribute is StorageItemTypes.File or StorageItemTypes.Folder)
                            {
                                items.Add(await ftpItem.ToStorageItem());
                            }
                        }
                        else if (listedItem.PrimaryItemAttribute == StorageItemTypes.File || listedItem is ZipItem)
                        {
                            var result = await associatedInstance.FilesystemViewModel.GetFileFromPathAsync(listedItem.ItemPath)
                                .OnSuccess(t => items.Add(t));
                            if (!result)
                            {
                                throw new IOException($"Failed to process {listedItem.ItemPath}.", (int)result.ErrorCode);
                            }
                        }
                        else
                        {
                            var result = await associatedInstance.FilesystemViewModel.GetFolderFromPathAsync(listedItem.ItemPath)
                                .OnSuccess(t => items.Add(t));
                            if (!result)
                            {
                                throw new IOException($"Failed to process {listedItem.ItemPath}.", (int)result.ErrorCode);
                            }
                        }
                    }, 10, banner?.CancellationToken ?? default);
                }
                catch (Exception ex)
                {
                    if (ex.HResult == (int)FileSystemStatusCode.Unauthorized)
                    {
                        // Try again with fulltrust process
                        var connection = await AppServiceConnectionHelper.Instance;
                        if (connection != null)
                        {
                            string filePaths = string.Join('|', associatedInstance.SlimContentPage.SelectedItems.Select(x => x.ItemPath));
                            AppServiceResponseStatus status = await connection.SendMessageAsync(new ValueSet()
                            {
                                { "Arguments", "FileOperation" },
                                { "fileop", "Clipboard" },
                                { "filepath", filePaths },
                                { "operation", (int)DataPackageOperation.Copy }
                            });
                            if (status == AppServiceResponseStatus.Success)
                            {
                                banner?.Remove();
                                return;
                            }
                        }
                    }
                    banner?.Remove();
                    return;
                }

                banner?.Remove();
            }

            var onlyStandard = items.All(x => x is StorageFile || x is StorageFolder || x is SystemStorageFile || x is SystemStorageFolder);
            if (onlyStandard)
            {
                items = new ConcurrentBag<IStorageItem>(await items.ToStandardStorageItemsAsync());
            }
            if (!items.Any())
            {
                return;
            }
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
            if (packageView && packageView.Result != null)
            {
                await associatedInstance.FilesystemHelpers.PerformOperationTypeAsync(packageView.Result.RequestedOperation, packageView, destinationPath, false, true);
                associatedInstance?.SlimContentPage?.ItemManipulationModel?.RefreshItemsOpacity();
            }
        }

        public static async Task<bool> RenameFileItemAsync(ListedItem item, string newName, IShellPage associatedInstance)
        {
            if (item is AlternateStreamItem ads) // For alternate streams ItemName is not a substring ItemNameRaw
            {
                newName = item.ItemNameRaw.Replace(
                    item.ItemName.Substring(item.ItemName.LastIndexOf(":") + 1),
                    newName.Substring(newName.LastIndexOf(":") + 1),
                    StringComparison.Ordinal);
                newName = $"{ads.MainStreamName}:{newName}";
            }
            else
            {
                newName = item.ItemNameRaw.Replace(item.ItemName, newName, StringComparison.Ordinal);
            }
            if (item.ItemNameRaw == newName || string.IsNullOrEmpty(newName))
            {
                return true;
            }

            ReturnResult renamed = ReturnResult.InProgress;
            if (item.PrimaryItemAttribute == StorageItemTypes.Folder)
            {
                renamed = await associatedInstance.FilesystemHelpers.RenameAsync(StorageHelpers.FromPathAndType(item.ItemPath, FilesystemItemType.Directory),
                    newName, NameCollisionOption.FailIfExists, true);
            }
            else
            {
                renamed = await associatedInstance.FilesystemHelpers.RenameAsync(StorageHelpers.FromPathAndType(item.ItemPath, FilesystemItemType.File),
                    newName, NameCollisionOption.FailIfExists, true);
            }

            if (renamed == ReturnResult.Success)
            {
                associatedInstance.ToolbarViewModel.CanGoForward = false;
                return true;
            }
            return false;
        }

        public static async void CreateFileFromDialogResultType(AddItemDialogItemType itemType, ShellNewEntry itemInfo, IShellPage associatedInstance)
        {
            _ = await CreateFileFromDialogResultTypeForResult(itemType, itemInfo, associatedInstance);
        }

        public static async Task<IStorageItem> CreateFileFromDialogResultTypeForResult(AddItemDialogItemType itemType, ShellNewEntry itemInfo, IShellPage associatedInstance)
        {
            string currentPath = null;
            if (associatedInstance.SlimContentPage != null)
            {
                currentPath = associatedInstance.FilesystemViewModel.WorkingDirectory;
                if (App.LibraryManager.TryGetLibrary(currentPath, out var library))
                {
                    if (!library.IsEmpty && library.Folders.Count == 1) // TODO: handle libraries with multiple folders
                    {
                        currentPath = library.Folders.First();
                    }
                }
            }

            // Skip rename dialog when ShellNewEntry has a Command (e.g. ".accdb", ".gdoc")
            string userInput = null;
            if (itemType != AddItemDialogItemType.File || itemInfo?.Command == null)
            {
                DynamicDialog dialog = DynamicDialogFactory.GetFor_RenameDialog();
                await dialog.ShowAsync(); // Show rename dialog

                if (dialog.DynamicResult != DynamicDialogResult.Primary)
                {
                    return null;
                }

                userInput = dialog.ViewModel.AdditionalData as string;
            }

            // Create file based on dialog result
            (ReturnResult Status, IStorageItem Item) created = (ReturnResult.Failed, null);
            switch (itemType)
            {
                case AddItemDialogItemType.Folder:
                    userInput = !string.IsNullOrWhiteSpace(userInput) ? userInput : "NewFolder".GetLocalized();
                    created = await associatedInstance.FilesystemHelpers.CreateAsync(
                        StorageHelpers.FromPathAndType(PathNormalization.Combine(currentPath, userInput), FilesystemItemType.Directory),
                        true);
                    break;

                case AddItemDialogItemType.File:
                    userInput = !string.IsNullOrWhiteSpace(userInput) ? userInput : itemInfo?.Name ?? "NewFile".GetLocalized();
                    created = await associatedInstance.FilesystemHelpers.CreateAsync(
                        StorageHelpers.FromPathAndType(PathNormalization.Combine(currentPath, userInput + itemInfo?.Extension), FilesystemItemType.File),
                        true);
                    break;
            }

            if (created.Status == ReturnResult.AccessUnauthorized)
            {
                await DialogDisplayHelper.ShowDialogAsync("AccessDenied".GetLocalized(), "AccessDeniedCreateDialog/Text".GetLocalized());
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
                if (folder == null)
                {
                    return;
                }
                await associatedInstance.FilesystemHelpers.MoveItemsAsync(items, items.Select(x => PathNormalization.Combine(folder.Path, x.Name)), false, true);
            }
            catch (Exception ex)
            {
                App.Logger.Warn(ex);
            }
        }

        /// <summary>
        /// Set a single file or folder to hidden or unhidden an refresh the
        /// view after setting the flag
        /// </summary>
        /// <param name="item"></param>
        /// <param name="isHidden"></param>
        public static void SetHiddenAttributeItem(ListedItem item, bool isHidden, ItemManipulationModel itemManipulationModel)
        {
            item.IsHiddenItem = isHidden;
            itemManipulationModel.RefreshItemsOpacity();
        }
    }
}