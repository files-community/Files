﻿using Files.Common;
using Files.Dialogs;
using Files.Enums;
using Files.Extensions;
using Files.Filesystem;
using Files.Filesystem.StorageItems;
using Files.Helpers;
using Files.ViewModels;
using Files.ViewModels.Dialogs;
using Files.Views;
using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.DragDrop;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Files.Interacts
{
    /// <summary>
    /// This class provides default implementation for BaseLayout commands.
    /// This class can be also inherited from and functions overridden to provide custom functionality
    /// </summary>
    public class BaseLayoutCommandImplementationModel : IBaseLayoutCommandImplementationModel
    {
        #region Singleton

        private IBaseLayout SlimContentPage => associatedInstance?.SlimContentPage;

        private IFilesystemHelpers FilesystemHelpers => associatedInstance?.FilesystemHelpers;

        #endregion Singleton

        #region Private Members

        private readonly IShellPage associatedInstance;

        private readonly ItemManipulationModel itemManipulationModel;

        #endregion Private Members

        #region Constructor

        public BaseLayoutCommandImplementationModel(IShellPage associatedInstance, ItemManipulationModel itemManipulationModel)
        {
            this.associatedInstance = associatedInstance;
            this.itemManipulationModel = itemManipulationModel;
        }

        #endregion Constructor

        #region IDisposable

        public void Dispose()
        {
            //associatedInstance = null;
        }

        #endregion IDisposable

        #region Command Implementation

        public virtual void RenameItem(RoutedEventArgs e)
        {
            itemManipulationModel.StartRenameItem();
        }

        public virtual async void CreateShortcut(RoutedEventArgs e)
        {
            foreach (ListedItem selectedItem in SlimContentPage.SelectedItems)
            {
                var connection = await AppServiceConnectionHelper.Instance;
                if (connection != null)
                {
                    var value = new ValueSet()
                    {
                        { "Arguments", "FileOperation" },
                        { "fileop", "CreateLink" },
                        { "targetpath", selectedItem.ItemPath },
                        { "arguments", "" },
                        { "workingdir", "" },
                        { "runasadmin", false },
                        {
                            "filepath",
                            Path.Combine(associatedInstance.FilesystemViewModel.WorkingDirectory,
                                string.Format("ShortcutCreateNewSuffix".GetLocalized(), selectedItem.ItemName) + ".lnk")
                        }
                    };
                    await connection.SendMessageAsync(value);
                }
            }
        }

        public virtual void SetAsLockscreenBackgroundItem(RoutedEventArgs e)
        {
            WallpaperHelpers.SetAsBackground(WallpaperType.LockScreen, SlimContentPage.SelectedItem.ItemPath, associatedInstance);
        }

        public virtual void SetAsDesktopBackgroundItem(RoutedEventArgs e)
        {
            WallpaperHelpers.SetAsBackground(WallpaperType.Desktop, SlimContentPage.SelectedItem.ItemPath, associatedInstance);
        }

        public virtual async void RunAsAdmin(RoutedEventArgs e)
        {
            var connection = await AppServiceConnectionHelper.Instance;
            if (connection != null)
            {
                await connection.SendMessageAsync(new ValueSet()
                {
                    { "Arguments", "InvokeVerb" },
                    { "FilePath", SlimContentPage.SelectedItem.ItemPath },
                    { "Verb", "runas" }
                });
            }
        }

        public virtual async void RunAsAnotherUser(RoutedEventArgs e)
        {
            var connection = await AppServiceConnectionHelper.Instance;
            if (connection != null)
            {
                await connection.SendMessageAsync(new ValueSet()
                {
                    { "Arguments", "InvokeVerb" },
                    { "FilePath", SlimContentPage.SelectedItem.ItemPath },
                    { "Verb", "runasuser" }
                });
            }
        }

        public virtual void SidebarPinItem(RoutedEventArgs e)
        {
            SidebarHelpers.PinItems(SlimContentPage.SelectedItems);
        }

        public virtual void SidebarUnpinItem(RoutedEventArgs e)
        {
            SidebarHelpers.UnpinItems(SlimContentPage.SelectedItems);
        }

        public virtual void OpenItem(RoutedEventArgs e)
        {
            NavigationHelpers.OpenSelectedItems(associatedInstance, false);
        }

        public virtual void UnpinDirectoryFromFavorites(RoutedEventArgs e)
        {
            App.SidebarPinnedController.Model.RemoveItem(associatedInstance.FilesystemViewModel.WorkingDirectory);
        }

        public virtual async void EmptyRecycleBin(RoutedEventArgs e)
        {
            await RecycleBinHelpers.S_EmptyRecycleBin();
        }

        public virtual async void QuickLook(RoutedEventArgs e)
        {
            await QuickLookHelpers.ToggleQuickLook(associatedInstance);
        }

        public virtual async void CopyItem(RoutedEventArgs e)
        {
            await UIFilesystemHelpers.CopyItem(associatedInstance);
        }

        public virtual void CutItem(RoutedEventArgs e)
        {
            UIFilesystemHelpers.CutItem(associatedInstance);
        }

        public virtual async void RestoreItem(RoutedEventArgs e)
        {
            var items = SlimContentPage.SelectedItems.ToList().Where(x => x is RecycleBinItem).Select((item) => new
            {
                Source = StorageHelpers.FromPathAndType(
                    item.ItemPath,
                    item.PrimaryItemAttribute == StorageItemTypes.File ? FilesystemItemType.File : FilesystemItemType.Directory),
                Dest = (item as RecycleBinItem).ItemOriginalPath
            });
            await FilesystemHelpers.RestoreItemsFromTrashAsync(items.Select(x => x.Source), items.Select(x => x.Dest), true);
        }

        public virtual async void DeleteItem(RoutedEventArgs e)
        {
            var items = SlimContentPage.SelectedItems.ToList().Select((item) => StorageHelpers.FromPathAndType(
                item.ItemPath,
                item.PrimaryItemAttribute == StorageItemTypes.File ? FilesystemItemType.File : FilesystemItemType.Directory));
            await FilesystemHelpers.DeleteItemsAsync(items, true, false, true);
        }

        public virtual void ShowFolderProperties(RoutedEventArgs e)
        {
            FilePropertiesHelpers.ShowProperties(associatedInstance);
        }

        public virtual void ShowProperties(RoutedEventArgs e)
        {
            FilePropertiesHelpers.ShowProperties(associatedInstance);
        }

        public virtual async void OpenFileLocation(RoutedEventArgs e)
        {
            ShortcutItem item = SlimContentPage.SelectedItem as ShortcutItem;

            if (string.IsNullOrWhiteSpace(item?.TargetPath))
            {
                return;
            }

            // Check if destination path exists
            string folderPath = Path.GetDirectoryName(item.TargetPath);
            FilesystemResult<StorageFolderWithPath> destFolder = await associatedInstance.FilesystemViewModel.GetFolderWithPathFromPathAsync(folderPath);

            if (destFolder)
            {
                associatedInstance.NavigateWithArguments(associatedInstance.InstanceViewModel.FolderSettings.GetLayoutType(folderPath), new NavigationArguments()
                {
                    NavPathParam = folderPath,
                    SelectItems = new[] { Path.GetFileName(item.TargetPath.TrimPath()) },
                    AssociatedTabInstance = associatedInstance
                });
            }
            else if (destFolder == FileSystemStatusCode.NotFound)
            {
                await DialogDisplayHelper.ShowDialogAsync("FileNotFoundDialog/Title".GetLocalized(), "FileNotFoundDialog/Text".GetLocalized());
            }
            else
            {
                await DialogDisplayHelper.ShowDialogAsync("InvalidItemDialogTitle".GetLocalized(),
                    string.Format("InvalidItemDialogContent".GetLocalized(), Environment.NewLine, destFolder.ErrorCode.ToString()));
            }
        }

        public virtual void OpenParentFolder(RoutedEventArgs e)
        {
            var item = SlimContentPage.SelectedItem;
            var folderPath = Path.GetDirectoryName(item.ItemPath.TrimEnd('\\'));
            associatedInstance.NavigateWithArguments(associatedInstance.InstanceViewModel.FolderSettings.GetLayoutType(folderPath), new NavigationArguments()
            {
                NavPathParam = folderPath,
                SelectItems = new[] { item.ItemNameRaw },
                AssociatedTabInstance = associatedInstance
            });
        }

        public virtual void OpenItemWithApplicationPicker(RoutedEventArgs e)
        {
            NavigationHelpers.OpenSelectedItems(associatedInstance, true);
        }

        public virtual async void OpenDirectoryInNewTab(RoutedEventArgs e)
        {
            foreach (ListedItem listedItem in SlimContentPage.SelectedItems)
            {
                await CoreWindow.GetForCurrentThread().Dispatcher.RunAsync(CoreDispatcherPriority.Low, async () =>
                {
                    await MainPageViewModel.AddNewTabByPathAsync(typeof(PaneHolderPage), (listedItem as ShortcutItem)?.TargetPath ?? listedItem.ItemPath);
                });
            }
        }

        public virtual void OpenDirectoryInNewPane(RoutedEventArgs e)
        {
            ListedItem listedItem = SlimContentPage.SelectedItems.FirstOrDefault();
            if (listedItem != null)
            {
                associatedInstance.PaneHolder?.OpenPathInNewPane((listedItem as ShortcutItem)?.TargetPath ?? listedItem.ItemPath);
            }
        }

        public virtual async void OpenInNewWindowItem(RoutedEventArgs e)
        {
            List<ListedItem> items = SlimContentPage.SelectedItems;
            foreach (ListedItem listedItem in items)
            {
                var selectedItemPath = (listedItem as ShortcutItem)?.TargetPath ?? listedItem.ItemPath;
                var folderUri = new Uri($"files-uwp:?folder={@selectedItemPath}");
                await Launcher.LaunchUriAsync(folderUri);
            }
        }

        public virtual void CreateNewFolder(RoutedEventArgs e)
        {
            UIFilesystemHelpers.CreateFileFromDialogResultType(AddItemType.Folder, null, associatedInstance);
        }

        public virtual void CreateNewFile(ShellNewEntry f)
        {
            UIFilesystemHelpers.CreateFileFromDialogResultType(AddItemType.File, f, associatedInstance);
        }

        public virtual async void PasteItemsFromClipboard(RoutedEventArgs e)
        {
            if (SlimContentPage.SelectedItems.Count == 1 && SlimContentPage.SelectedItems.Single().PrimaryItemAttribute == StorageItemTypes.Folder)
            {
                await UIFilesystemHelpers.PasteItemAsync(SlimContentPage.SelectedItems.Single().ItemPath, associatedInstance);
            }
            else
            {
                await UIFilesystemHelpers.PasteItemAsync(associatedInstance.FilesystemViewModel.WorkingDirectory, associatedInstance);
            }
        }

        public virtual void CopyPathOfSelectedItem(RoutedEventArgs e)
        {
            try
            {
                if (SlimContentPage != null)
                {
                    var path = SlimContentPage.SelectedItem != null ? SlimContentPage.SelectedItem.ItemPath : associatedInstance.FilesystemViewModel.WorkingDirectory;
                    if (FtpHelpers.IsFtpPath(path))
                    {
                        path = path.Replace("\\", "/", StringComparison.Ordinal);
                    }
                    DataPackage data = new();
                    data.SetText(path);
                    Clipboard.SetContent(data);
                    Clipboard.Flush();
                }
            }
            catch (Exception)
            {
                Debugger.Break();
            }
        }

        public virtual async void OpenDirectoryInDefaultTerminal(RoutedEventArgs e)
        {
            await NavigationHelpers.OpenDirectoryInTerminal(associatedInstance.FilesystemViewModel.WorkingDirectory);
        }

        public virtual void ShareItem(RoutedEventArgs e)
        {
            DataTransferManager manager = DataTransferManager.GetForCurrentView();
            manager.DataRequested += new TypedEventHandler<DataTransferManager, DataRequestedEventArgs>(Manager_DataRequested);
            DataTransferManager.ShowShareUI(new ShareUIOptions
            {
                Theme = Enum.IsDefined(typeof(ShareUITheme), ThemeHelper.RootTheme.ToString()) ? (ShareUITheme)ThemeHelper.RootTheme : ShareUITheme.Default
            });

            async void Manager_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
            {
                DataRequestDeferral dataRequestDeferral = args.Request.GetDeferral();
                List<IStorageItem> items = new();
                DataRequest dataRequest = args.Request;

                /*dataRequest.Data.Properties.Title = "Data Shared From Files";
                dataRequest.Data.Properties.Description = "The items you selected will be shared";*/

                foreach (ListedItem item in SlimContentPage.SelectedItems)
                {
                    if (item is ShortcutItem shItem)
                    {
                        if (shItem.IsLinkItem && !string.IsNullOrEmpty(shItem.TargetPath))
                        {
                            dataRequest.Data.Properties.Title = string.Format("ShareDialogTitle".GetLocalized(), item.ItemName);
                            dataRequest.Data.Properties.Description = "ShareDialogSingleItemDescription".GetLocalized();
                            dataRequest.Data.SetWebLink(new Uri(shItem.TargetPath));
                            dataRequestDeferral.Complete();
                            return;
                        }
                    }
                    else if (item.PrimaryItemAttribute == StorageItemTypes.Folder && !item.IsZipItem)
                    {
                        if (await StorageHelpers.ToStorageItem<BaseStorageFolder>(item.ItemPath, associatedInstance) is BaseStorageFolder folder)
                        {
                            items.Add(folder);
                        }
                    }
                    else
                    {
                        if (await StorageHelpers.ToStorageItem<BaseStorageFile>(item.ItemPath, associatedInstance) is BaseStorageFile file)
                        {
                            items.Add(file);
                        }
                    }
                }

                if (items.Count == 1)
                {
                    dataRequest.Data.Properties.Title = string.Format("ShareDialogTitle".GetLocalized(), items.First().Name);
                    dataRequest.Data.Properties.Description = "ShareDialogSingleItemDescription".GetLocalized();
                }
                else if (items.Count == 0)
                {
                    dataRequest.FailWithDisplayText("ShareDialogFailMessage".GetLocalized());
                    dataRequestDeferral.Complete();
                    return;
                }
                else
                {
                    dataRequest.Data.Properties.Title = string.Format("ShareDialogTitleMultipleItems".GetLocalized(), items.Count,
                        "ItemsCount.Text".GetLocalized());
                    dataRequest.Data.Properties.Description = "ShareDialogMultipleItemsDescription".GetLocalized();
                }

                dataRequest.Data.SetStorageItems(items, false);
                dataRequestDeferral.Complete();

                // TODO: Unhook the event somewhere
            }
        }

        public virtual void PinDirectoryToFavorites(RoutedEventArgs e)
        {
            App.SidebarPinnedController.Model.AddItem(associatedInstance.FilesystemViewModel.WorkingDirectory);
        }

        public virtual async void ItemPointerPressed(PointerRoutedEventArgs e)
        {
            if (e.GetCurrentPoint(null).Properties.IsMiddleButtonPressed)
            {
                if ((e.OriginalSource as FrameworkElement)?.DataContext is ListedItem Item && Item.PrimaryItemAttribute == StorageItemTypes.Folder)
                {
                    // If a folder item was clicked, disable middle mouse click to scroll to cancel the mouse scrolling state and re-enable it
                    SlimContentPage.IsMiddleClickToScrollEnabled = false;
                    SlimContentPage.IsMiddleClickToScrollEnabled = true;

                    if (Item.IsShortcutItem)
                    {
                        await NavigationHelpers.OpenPathInNewTab(((e.OriginalSource as FrameworkElement)?.DataContext as ShortcutItem)?.TargetPath ?? Item.ItemPath);
                    }
                    else
                    {
                        await NavigationHelpers.OpenPathInNewTab(Item.ItemPath);
                    }
                }
            }
        }

        public virtual async void UnpinItemFromStart(RoutedEventArgs e)
        {
            if (associatedInstance.SlimContentPage.SelectedItems.Count > 0)
            {
                foreach (ListedItem listedItem in associatedInstance.SlimContentPage.SelectedItems)
                {
                    await App.SecondaryTileHelper.UnpinFromStartAsync(listedItem.ItemPath);
                }
            }
            else
            {
                await App.SecondaryTileHelper.UnpinFromStartAsync(associatedInstance.FilesystemViewModel.WorkingDirectory);
            }
        }

        public async void PinItemToStart(RoutedEventArgs e)
        {
            if (associatedInstance.SlimContentPage.SelectedItems.Count > 0)
            {
                foreach (ListedItem listedItem in associatedInstance.SlimContentPage.SelectedItems)
                {
                    await App.SecondaryTileHelper.TryPinFolderAsync(listedItem.ItemPath, listedItem.ItemName);
                }
            }
            else
            {
                await App.SecondaryTileHelper.TryPinFolderAsync(associatedInstance.FilesystemViewModel.CurrentFolder.ItemPath, associatedInstance.FilesystemViewModel.CurrentFolder.ItemName);
            }
        }

        public virtual void PointerWheelChanged(PointerRoutedEventArgs e)
        {
            if (e.KeyModifiers == VirtualKeyModifiers.Control)
            {
                if (e.GetCurrentPoint(null).Properties.MouseWheelDelta < 0) // Mouse wheel down
                {
                    GridViewSizeDecrease(null);
                }
                else // Mouse wheel up
                {
                    GridViewSizeIncrease(null);
                }

                e.Handled = true;
            }
        }

        public virtual void GridViewSizeDecrease(KeyboardAcceleratorInvokedEventArgs e)
        {
            if (associatedInstance.IsCurrentInstance)
            {
                associatedInstance.InstanceViewModel.FolderSettings.GridViewSize = associatedInstance.InstanceViewModel.FolderSettings.GridViewSize - Constants.Browser.GridViewBrowser.GridViewIncrement; // Make Smaller
            }
            if (e != null)
            {
                e.Handled = true;
            }
        }

        public virtual void GridViewSizeIncrease(KeyboardAcceleratorInvokedEventArgs e)
        {
            if (associatedInstance.IsCurrentInstance)
            {
                associatedInstance.InstanceViewModel.FolderSettings.GridViewSize = associatedInstance.InstanceViewModel.FolderSettings.GridViewSize + Constants.Browser.GridViewBrowser.GridViewIncrement; // Make Larger
            }
            if (e != null)
            {
                e.Handled = true;
            }
        }

        public virtual async Task DragOver(DragEventArgs e)
        {
            var deferral = e.GetDeferral();

            if (associatedInstance.InstanceViewModel.IsPageTypeSearchResults)
            {
                e.AcceptedOperation = DataPackageOperation.None;
                deferral.Complete();
                return;
            }

            itemManipulationModel.ClearSelection();

            if (Filesystem.FilesystemHelpers.HasDraggedStorageItems(e.DataView))
            {
                e.Handled = true;

                var handledByFtp = await Filesystem.FilesystemHelpers.CheckDragNeedsFulltrust(e.DataView);
                var draggedItems = await Filesystem.FilesystemHelpers.GetDraggedStorageItems(e.DataView);

                var pwd = associatedInstance.FilesystemViewModel.WorkingDirectory.TrimPath();
                var folderName = (Path.IsPathRooted(pwd) && Path.GetPathRoot(pwd) == pwd) ? Path.GetPathRoot(pwd) : Path.GetFileName(pwd);

                // As long as one file doesn't already belong to this folder
                if (associatedInstance.InstanceViewModel.IsPageTypeSearchResults || (draggedItems.Any() && draggedItems.AreItemsAlreadyInFolder(associatedInstance.FilesystemViewModel.WorkingDirectory)))
                {
                    e.AcceptedOperation = DataPackageOperation.None;
                }
                else if (handledByFtp)
                {
                    if (pwd.StartsWith(CommonPaths.RecycleBinPath, StringComparison.Ordinal))
                    {
                        e.AcceptedOperation = DataPackageOperation.None;
                    }
                    else
                    {
                        e.DragUIOverride.IsCaptionVisible = true;
                        e.DragUIOverride.Caption = string.Format("CopyToFolderCaptionText".GetLocalized(), folderName);
                        e.AcceptedOperation = DataPackageOperation.Copy;
                    }
                }
                else if (!draggedItems.Any())
                {
                    e.AcceptedOperation = DataPackageOperation.None;
                }
                else
                {
                    e.DragUIOverride.IsCaptionVisible = true;
                    if (pwd.StartsWith(CommonPaths.RecycleBinPath, StringComparison.Ordinal))
                    {
                        e.DragUIOverride.Caption = string.Format("MoveToFolderCaptionText".GetLocalized(), folderName);
                        e.AcceptedOperation = DataPackageOperation.Move;
                    }
                    else if (e.Modifiers.HasFlag(DragDropModifiers.Alt) || e.Modifiers.HasFlag(DragDropModifiers.Control | DragDropModifiers.Shift))
                    {
                        e.DragUIOverride.Caption = string.Format("LinkToFolderCaptionText".GetLocalized(), folderName);
                        e.AcceptedOperation = DataPackageOperation.Link;
                    }
                    else if (e.Modifiers.HasFlag(DragDropModifiers.Control))
                    {
                        e.DragUIOverride.Caption = string.Format("CopyToFolderCaptionText".GetLocalized(), folderName);
                        e.AcceptedOperation = DataPackageOperation.Copy;
                    }
                    else if (e.Modifiers.HasFlag(DragDropModifiers.Shift))
                    {
                        e.DragUIOverride.Caption = string.Format("MoveToFolderCaptionText".GetLocalized(), folderName);
                        e.AcceptedOperation = DataPackageOperation.Move;
                    }
                    else if (draggedItems.Any(x => x.Item is ZipStorageFile || x.Item is ZipStorageFolder)
                        || ZipStorageFolder.IsZipPath(pwd))
                    {
                        e.DragUIOverride.Caption = string.Format("CopyToFolderCaptionText".GetLocalized(), folderName);
                        e.AcceptedOperation = DataPackageOperation.Copy;
                    }
                    else if (draggedItems.AreItemsInSameDrive(associatedInstance.FilesystemViewModel.WorkingDirectory))
                    {
                        e.DragUIOverride.Caption = string.Format("MoveToFolderCaptionText".GetLocalized(), folderName);
                        e.AcceptedOperation = DataPackageOperation.Move;
                    }
                    else
                    {
                        e.DragUIOverride.Caption = string.Format("CopyToFolderCaptionText".GetLocalized(), folderName);
                        e.AcceptedOperation = DataPackageOperation.Copy;
                    }
                }
            }

            deferral.Complete();
        }

        public virtual async Task Drop(DragEventArgs e)
        {
            var deferral = e.GetDeferral();

            if (Filesystem.FilesystemHelpers.HasDraggedStorageItems(e.DataView))
            {
                await associatedInstance.FilesystemHelpers.PerformOperationTypeAsync(e.AcceptedOperation, e.DataView, associatedInstance.FilesystemViewModel.WorkingDirectory, false, true);
                e.Handled = true;
            }

            deferral.Complete();
        }

        public virtual void RefreshItems(RoutedEventArgs e)
        {
            associatedInstance.Refresh_Click();
        }

        public void SearchUnindexedItems(RoutedEventArgs e)
        {
            associatedInstance.SubmitSearch(associatedInstance.InstanceViewModel.CurrentSearchQuery, true);
        }

        public async Task CreateFolderWithSelection(RoutedEventArgs e)
        {
            await UIFilesystemHelpers.CreateFolderWithSelectionAsync(associatedInstance);
        }

        public async Task DecompressArchive()
        {
            BaseStorageFile archive = await StorageHelpers.ToStorageItem<BaseStorageFile>(associatedInstance.SlimContentPage.SelectedItem.ItemPath);

            if (archive != null)
            {
                DecompressArchiveDialog decompressArchiveDialog = new();
                DecompressArchiveDialogViewModel decompressArchiveViewModel = new(archive);
                decompressArchiveDialog.ViewModel = decompressArchiveViewModel;

                ContentDialogResult option = await decompressArchiveDialog.ShowAsync();

                if (option == ContentDialogResult.Primary)
                {
                    // Check if archive still exists
                    if (!StorageHelpers.Exists(archive.Path))
                    {
                        return;
                    }

                    CancellationTokenSource extractCancellation = new();
                    PostedStatusBanner banner = App.OngoingTasksViewModel.PostOperationBanner(
                        string.Empty,
                        "ExtractingArchiveText".GetLocalized(),
                        0,
                        ReturnResult.InProgress,
                        FileOperationType.Extract,
                        extractCancellation);

                    BaseStorageFolder destinationFolder = decompressArchiveViewModel.DestinationFolder;
                    string destinationFolderPath = decompressArchiveViewModel.DestinationFolderPath;

                    if (destinationFolder == null)
                    {
                        BaseStorageFolder parentFolder = await StorageHelpers.ToStorageItem<BaseStorageFolder>(Path.GetDirectoryName(archive.Path));
                        destinationFolder = await FilesystemTasks.Wrap(() => parentFolder.CreateFolderAsync(Path.GetFileName(destinationFolderPath), CreationCollisionOption.GenerateUniqueName).AsTask());
                    }
                    if (destinationFolder == null)
                    {
                        return; // Could not create dest folder
                    }

                    Stopwatch sw = new();
                    sw.Start();

                    await ZipHelpers.ExtractArchive(archive, destinationFolder, banner.Progress, extractCancellation.Token);

                    sw.Stop();
                    banner.Remove();

                    if (sw.Elapsed.TotalSeconds >= 6)
                    {
                        App.OngoingTasksViewModel.PostBanner(
                            "ExtractingCompleteText".GetLocalized(),
                            "ArchiveExtractionCompletedSuccessfullyText".GetLocalized(),
                            0,
                            ReturnResult.Success,
                            FileOperationType.Extract);
                    }

                    if (decompressArchiveViewModel.OpenDestinationFolderOnCompletion)
                    {
                        await NavigationHelpers.OpenPath(destinationFolderPath, associatedInstance, FilesystemItemType.Directory);
                    }
                }
            }
        }

        public async Task DecompressArchiveHere()
        {
            BaseStorageFile archive = await StorageHelpers.ToStorageItem<BaseStorageFile>(associatedInstance.SlimContentPage.SelectedItem.ItemPath);
            BaseStorageFolder currentFolder = await StorageHelpers.ToStorageItem<BaseStorageFolder>(associatedInstance.FilesystemViewModel.CurrentFolder.ItemPath);

            if (archive != null && currentFolder != null)
            {
                CancellationTokenSource extractCancellation = new();
                PostedStatusBanner banner = App.OngoingTasksViewModel.PostOperationBanner(
                    string.Empty,
                    "ExtractingArchiveText".GetLocalized(),
                    0,
                    ReturnResult.InProgress,
                    FileOperationType.Extract,
                    extractCancellation);

                Stopwatch sw = new();
                sw.Start();

                await ZipHelpers.ExtractArchive(archive, currentFolder, banner.Progress, extractCancellation.Token);

                sw.Stop();
                banner.Remove();

                if (sw.Elapsed.TotalSeconds >= 6)
                {
                    App.OngoingTasksViewModel.PostBanner(
                        "ExtractingCompleteText".GetLocalized(),
                        "ArchiveExtractionCompletedSuccessfullyText".GetLocalized(),
                        0,
                        ReturnResult.Success,
                        FileOperationType.Extract);
                }
            }
        }

        public async Task DecompressArchiveToChildFolder()
        {
            var selectedItem = associatedInstance?.SlimContentPage?.SelectedItem;
            if (selectedItem == null)
            {
                return;
            }

            BaseStorageFile archive = await StorageHelpers.ToStorageItem<BaseStorageFile>(selectedItem.ItemPath);
            BaseStorageFolder currentFolder = await StorageHelpers.ToStorageItem<BaseStorageFolder>(associatedInstance.FilesystemViewModel.CurrentFolder.ItemPath);
            BaseStorageFolder destinationFolder = null;

            if (currentFolder != null)
            {
                destinationFolder = await FilesystemTasks.Wrap(() => currentFolder.CreateFolderAsync(Path.GetFileNameWithoutExtension(archive.Path), CreationCollisionOption.OpenIfExists).AsTask());
            }

            if (archive != null && destinationFolder != null)
            {
                CancellationTokenSource extractCancellation = new();
                PostedStatusBanner banner = App.OngoingTasksViewModel.PostOperationBanner(
                    string.Empty,
                    "ExtractingArchiveText".GetLocalized(),
                    0,
                    ReturnResult.InProgress,
                    FileOperationType.Extract,
                    extractCancellation);

                Stopwatch sw = new();
                sw.Start();

                await ZipHelpers.ExtractArchive(archive, destinationFolder, banner.Progress, extractCancellation.Token);

                sw.Stop();
                banner.Remove();

                if (sw.Elapsed.TotalSeconds >= 6)
                {
                    App.OngoingTasksViewModel.PostBanner(
                        "ExtractingCompleteText".GetLocalized(),
                        "ArchiveExtractionCompletedSuccessfullyText".GetLocalized(),
                        0,
                        ReturnResult.Success,
                        FileOperationType.Extract);
                }
            }
        }

        public async Task InstallInfDriver()
        {
            var connection = await AppServiceConnectionHelper.Instance;
            if (connection != null)
            {
                foreach (ListedItem selectedItem in SlimContentPage.SelectedItems)
                {
                    var value = new ValueSet
                    {
                        { "Arguments", "InstallOperation" },
                        { "installop", "InstallInf" },
                        { "filepath", selectedItem.ItemPath },
                        { "extension", selectedItem.FileExtension },
                    };
                    await connection.SendMessageAsync(value);
                }
            }
        }

        public async Task RotateImageLeft()
        {
            await BitmapHelper.Rotate(PathNormalization.NormalizePath(SlimContentPage?.SelectedItems.First().ItemPath), BitmapRotation.Clockwise270Degrees);
            SlimContentPage?.ItemManipulationModel.RefreshItemsThumbnail();
            SlimContentPage?.PreviewPaneViewModel.UpdateSelectedItemPreview();
        }

        public async Task RotateImageRight()
        {
            await BitmapHelper.Rotate(PathNormalization.NormalizePath(SlimContentPage?.SelectedItems.First().ItemPath), BitmapRotation.Clockwise90Degrees);
            SlimContentPage?.ItemManipulationModel.RefreshItemsThumbnail();
            SlimContentPage?.PreviewPaneViewModel.UpdateSelectedItemPreview();
        }

        public async Task InstallFont()
        {
            foreach (ListedItem selectedItem in SlimContentPage.SelectedItems)
            {
                var connection = await AppServiceConnectionHelper.Instance;
                if (connection != null)
                {
                    var value = new ValueSet
                    {
                        { "Arguments", "InstallOperation" },
                        { "installop", "InstallFont" },
                        { "filepath", selectedItem.ItemPath },
                        { "extension", selectedItem.FileExtension },
                    };
                    await connection.SendMessageAsync(value);
                }
            }
        }

        #endregion Command Implementation
    }
}