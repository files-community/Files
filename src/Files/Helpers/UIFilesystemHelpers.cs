﻿using Files.Common;
using Files.Dialogs;
using Files.Enums;
using Files.Extensions;
using Files.Filesystem;
using Files.Filesystem.StorageItems;
using Files.Interacts;
using Files.ViewModels;
using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation.Collections;
using Windows.Storage;

namespace Files.Helpers
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
                    await associatedInstance.SlimContentPage.SelectedItems.ToList().ParallelForEach(async listedItem =>
                    {
                        if (banner != null)
                        {
                            ((IProgress<float>)banner.Progress).Report(items.Count / (float)itemsCount * 100);
                        }

                        // FTP don't support cut, fallback to copy
                        if (listedItem is not FtpItem)
                        {
                            _ = CoreApplication.MainView.DispatcherQueue.TryEnqueue(Windows.System.DispatcherQueuePriority.Low, () =>
                           {
                                // Dim opacities accordingly
                                listedItem.Opacity = Constants.UI.DimItemOpacity;
                           });
                        }
                        if (listedItem is FtpItem ftpItem)
                        {
                            if (listedItem.PrimaryItemAttribute == StorageItemTypes.File)
                            {
                                items.Add(await new FtpStorageFile(ftpItem).ToStorageFileAsync());
                            }
                            else if (listedItem.PrimaryItemAttribute == StorageItemTypes.Folder)
                            {
                                items.Add(new FtpStorageFolder(ftpItem));
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
                    await associatedInstance.SlimContentPage.SelectedItems.ToList().ParallelForEach(async listedItem =>
                    {
                        if (banner != null)
                        {
                            ((IProgress<float>)banner.Progress).Report(items.Count / (float)itemsCount * 100);
                        }

                        if (listedItem is FtpItem ftpItem)
                        {
                            if (listedItem.PrimaryItemAttribute == StorageItemTypes.File)
                            {
                                items.Add(await new FtpStorageFile(ftpItem).ToStorageFileAsync());
                            }
                            else if (listedItem.PrimaryItemAttribute == StorageItemTypes.Folder)
                            {
                                items.Add(new FtpStorageFolder(ftpItem));
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
            newName = item.ItemNameRaw.Replace(item.ItemName, newName, StringComparison.Ordinal);
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
                associatedInstance.NavToolbarViewModel.CanGoForward = false;
                return true;
            }
            return false;
        }

        public static async void CreateFileFromDialogResultType(AddItemType itemType, ShellNewEntry itemInfo, IShellPage associatedInstance)
        {
            _ = await CreateFileFromDialogResultTypeForResult(itemType, itemInfo, associatedInstance);
        }

        public static async Task<IStorageItem> CreateFileFromDialogResultTypeForResult(AddItemType itemType, ShellNewEntry itemInfo, IShellPage associatedInstance)
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

            // Show rename dialog
            DynamicDialog dialog = DynamicDialogFactory.GetFor_RenameDialog();
            await dialog.ShowAsync();

            if (dialog.DynamicResult != DynamicDialogResult.Primary)
            {
                return null;
            }

            // Create file based on dialog result
            string userInput = dialog.ViewModel.AdditionalData as string;
            var folderRes = await associatedInstance.FilesystemViewModel.GetFolderWithPathFromPathAsync(currentPath);
            var created = new FilesystemResult<(ReturnResult, IStorageItem)>((ReturnResult.Failed, null), FileSystemStatusCode.Generic);
            if (folderRes)
            {
                switch (itemType)
                {
                    case AddItemType.Folder:
                        userInput = !string.IsNullOrWhiteSpace(userInput) ? userInput : "NewFolder".GetLocalized();
                        created = await FilesystemTasks.Wrap(async () =>
                        {
                            return await associatedInstance.FilesystemHelpers.CreateAsync(
                                StorageHelpers.FromPathAndType(PathNormalization.Combine(folderRes.Result.Path, userInput), FilesystemItemType.Directory),
                                true);
                        });
                        break;

                    case AddItemType.File:
                        userInput = !string.IsNullOrWhiteSpace(userInput) ? userInput : itemInfo?.Name ?? "NewFile".GetLocalized();
                        created = await FilesystemTasks.Wrap(async () =>
                        {
                            return await associatedInstance.FilesystemHelpers.CreateAsync(
                                StorageHelpers.FromPathAndType(PathNormalization.Combine(folderRes.Result.Path, userInput + itemInfo?.Extension), FilesystemItemType.File),
                                true);
                        });
                        break;
                }
            }

            if (created == FileSystemStatusCode.Unauthorized)
            {
                await DialogDisplayHelper.ShowDialogAsync("AccessDenied".GetLocalized(), "AccessDeniedCreateDialog/Text".GetLocalized());
            }

            return created.Result.Item2;
        }

        public static async Task CreateFolderWithSelectionAsync(IShellPage associatedInstance)
        {
            try
            {
                await CopyItem(associatedInstance);
                var folder = await CreateFileFromDialogResultTypeForResult(AddItemType.Folder, null, associatedInstance);
                if (folder == null)
                {
                    return;
                }
                await associatedInstance.FilesystemHelpers.MoveItemsFromClipboard(Clipboard.GetContent(), folder.Path, false, true);
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