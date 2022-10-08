#nullable disable warnings

using Files.Shared;
using Files.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Files.Backend.Enums;

namespace Files.App.Interacts
{
    /// <summary>
    /// This class provides default implementation for BaseLayout commands.
    /// This class can be also inherited from and functions overridden to provide custom functionality
    /// </summary>
    public class BaseLayoutCommandImplementationModel : IBaseLayoutCommandImplementationModel
    {

        public BaseLayoutCommandImplementationModel()
        {
        }

        public void Dispose()
        {
        }

        
        public virtual void RenameItem()
        {
            itemManipulationModel.StartRenameItem();
        }

        public virtual async void CreateShortcut()
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
                            Path.Combine(associatedViewModel.FilesystemViewModel.WorkingDirectory,
                                string.Format("ShortcutCreateNewSuffix".GetLocalizedResource(), selectedItem.ItemName) + ".lnk")
                        }
                    };
                    await connection.SendMessageAsync(value);
                }
            }
        }

        public virtual void SetAsLockscreenBackgroundItem()
        {
            WallpaperHelpers.SetAsBackground(WallpaperType.LockScreen, SlimContentPage.SelectedItem.ItemPath);
        }

        public virtual void SetAsDesktopBackgroundItem()
        {
            WallpaperHelpers.SetAsBackground(WallpaperType.Desktop, SlimContentPage.SelectedItem.ItemPath);
        }

        public virtual void SetAsSlideshowItem()
        {
            var images = (from o in SlimContentPage.SelectedItems select o.ItemPath).ToArray();
            WallpaperHelpers.SetSlideshow(images);
        }

        public virtual async void RunAsAdmin()
        {
            await ContextMenu.InvokeVerb("runas", SlimContentPage.SelectedItem.ItemPath);
        }

        public virtual async void RunAsAnotherUser()
        {
            await ContextMenu.InvokeVerb("runasuser", SlimContentPage.SelectedItem.ItemPath);
        }

        public virtual void SidebarPinItem()
        {
            SidebarHelpers.PinItems(SlimContentPage.SelectedItems);
        }

        public virtual void SidebarUnpinItem()
        {
            SidebarHelpers.UnpinItems(SlimContentPage.SelectedItems);
        }

        public virtual void OpenItem()
        {
            NavigationHelpers.OpenSelectedItems(associatedViewModel, false);
        }

        public virtual void UnpinDirectoryFromFavorites()
        {
            App.SidebarPinnedController.Model.RemoveItem(associatedViewModel.FilesystemViewModel.WorkingDirectory);
        }

        public virtual async void EmptyRecycleBin()
        {
            await RecycleBinHelpers.S_EmptyRecycleBin();
        }

        public virtual async void RestoreRecycleBin()
        {
            await RecycleBinHelpers.S_RestoreRecycleBin(associatedViewModel);
        }

        public virtual async void RestoreSelectionRecycleBin()
        {
            await RecycleBinHelpers.S_RestoreSelectionRecycleBin(associatedViewModel);
        }

        public virtual async void QuickLook()
        {
            await QuickLookHelpers.ToggleQuickLook(associatedViewModel);
        }

        public virtual async void CopyItem()
        {
            await UIFilesystemHelpers.CopyItem(associatedViewModel);
        }

        public virtual void CutItem()
        {
            UIFilesystemHelpers.CutItem(associatedViewModel);
        }

        public virtual async void RestoreItem()
        {
            await RecycleBinHelpers.S_RestoreItem(associatedViewModel);
        }

        public virtual async void DeleteItem()
        {
            await RecycleBinHelpers.S_DeleteItem(associatedViewModel);
        }

        public virtual void ShowFolderProperties()
        {
            SlimContentPage.ItemContextMenuFlyout.Closed += OpenProperties;
        }

        public virtual void ShowProperties()
        {
            SlimContentPage.ItemContextMenuFlyout.Closed += OpenProperties;
        }

        private void OpenProperties(object sender, object e)
        {
            SlimContentPage.ItemContextMenuFlyout.Closed -= OpenProperties;
            FilePropertiesHelpers.ShowProperties(associatedViewModel);
        }

        public virtual async void OpenFileLocation()
        {
            ShortcutItem item = SlimContentPage.SelectedItem as ShortcutItem;

            if (string.IsNullOrWhiteSpace(item?.TargetPath))
            {
                return;
            }

            // Check if destination path exists
            string folderPath = Path.GetDirectoryName(item.TargetPath);
            FilesystemResult<StorageFolderWithPath> destFolder = await associatedViewModel.FilesystemViewModel.GetFolderWithPathFromPathAsync(folderPath);

            if (destFolder)
            {
                associatedViewModel.NavigateWithArguments(associatedViewModel.InstanceViewModel.FolderSettings.GetLayoutType(folderPath), new LayoutModeArguments()
                {
                    NavPathParam = folderPath,
                    SelectItems = new[] { Path.GetFileName(item.TargetPath.TrimPath()) },
                    AssociatedTabInstance = associatedViewModel
                });
            }
            else if (destFolder == FileSystemStatusCode.NotFound)
            {
                await DialogDisplayHelper.ShowDialogAsync("FileNotFoundDialog/Title".GetLocalizedResource(), "FileNotFoundDialog/Text".GetLocalizedResource());
            }
            else
            {
                await DialogDisplayHelper.ShowDialogAsync("InvalidItemDialogTitle".GetLocalizedResource(),
                    string.Format("InvalidItemDialogContent".GetLocalizedResource(), Environment.NewLine, destFolder.ErrorCode.ToString()));
            }
        }

        public virtual void OpenParentFolder()
        {
            var item = SlimContentPage.SelectedItem;
            var folderPath = Path.GetDirectoryName(item.ItemPath.TrimEnd('\\'));
            associatedViewModel.NavigateWithArguments(associatedViewModel.InstanceViewModel.FolderSettings.GetLayoutType(folderPath), new LayoutModeArguments()
            {
                NavPathParam = folderPath,
                SelectItems = new[] { item.ItemNameRaw },
                AssociatedTabInstance = associatedViewModel
            });
        }

        public virtual void OpenItemWithApplicationPicker()
        {
            NavigationHelpers.OpenSelectedItems(associatedViewModel, true);
        }

        public virtual async void OpenDirectoryInNewTab()
        {
            foreach (ListedItem listedItem in SlimContentPage.SelectedItems)
            {
                await App.Window.DispatcherQueue.EnqueueAsync(async () =>
                {
                    await MainPageViewModel.AddNewTabByPathAsync(typeof(PaneHolderPage), (listedItem as ShortcutItem)?.TargetPath ?? listedItem.ItemPath);
                }, Microsoft.UI.Dispatching.DispatcherQueuePriority.Low);
            }
        }

        public virtual void OpenDirectoryInNewPane()
        {
            ListedItem listedItem = SlimContentPage.SelectedItems.FirstOrDefault();
            if (listedItem != null)
            {
                associatedViewModel.PaneHolder?.OpenPathInNewPane((listedItem as ShortcutItem)?.TargetPath ?? listedItem.ItemPath);
            }
        }

        public virtual async void OpenInNewWindowItem()
        {
            List<ListedItem> items = SlimContentPage.SelectedItems;
            foreach (ListedItem listedItem in items)
            {
                var selectedItemPath = (listedItem as ShortcutItem)?.TargetPath ?? listedItem.ItemPath;
                var folderUri = new Uri($"files-uwp:?folder={@selectedItemPath}");
                await Launcher.LaunchUriAsync(folderUri);
            }
        }

        public virtual void CreateNewFolder()
        {
            UIFilesystemHelpers.CreateFileFromDialogResultType(AddItemDialogItemType.Folder, null, associatedViewModel);
        }

        public virtual void CreateNewFile(ShellNewEntry f)
        {
            UIFilesystemHelpers.CreateFileFromDialogResultType(AddItemDialogItemType.File, f, associatedViewModel);
        }

        public virtual async void PasteItemsFromClipboard()
        {
            if (SlimContentPage.SelectedItems.Count == 1 && SlimContentPage.SelectedItems.Single().PrimaryItemAttribute == StorageItemTypes.Folder)
            {
                await UIFilesystemHelpers.PasteItemAsync(SlimContentPage.SelectedItems.Single().ItemPath, associatedViewModel);
            }
            else
            {
                await UIFilesystemHelpers.PasteItemAsync(associatedViewModel.FilesystemViewModel.WorkingDirectory, associatedViewModel);
            }
        }

        public virtual void CopyPathOfSelectedItem()
        {
            try
            {
                if (SlimContentPage != null)
                {
                    var path = SlimContentPage.SelectedItem != null ? SlimContentPage.SelectedItem.ItemPath : associatedViewModel.FilesystemViewModel.WorkingDirectory;
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

        public virtual async void OpenDirectoryInDefaultTerminal()
        {
            await NavigationHelpers.OpenDirectoryInTerminal(associatedViewModel.FilesystemViewModel.WorkingDirectory);
        }

        public virtual void ShareItem()
        {
            var interop = DataTransferManager.As<UWPToWinAppSDKUpgradeHelpers.IDataTransferManagerInterop>();
            IntPtr result = interop.GetForWindow(App.WindowHandle, UWPToWinAppSDKUpgradeHelpers.InteropHelpers.DataTransferManagerInteropIID);
            var manager = WinRT.MarshalInterface<DataTransferManager>.FromAbi(result);
            manager.DataRequested += new TypedEventHandler<DataTransferManager, DataRequestedEventArgs>(Manager_DataRequested);

            interop.ShowShareUIForWindow(App.WindowHandle);

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
                            dataRequest.Data.Properties.Title = string.Format("ShareDialogTitle".GetLocalizedResource(), item.ItemName);
                            dataRequest.Data.Properties.Description = "ShareDialogSingleItemDescription".GetLocalizedResource();
                            dataRequest.Data.SetWebLink(new Uri(shItem.TargetPath));
                            dataRequestDeferral.Complete();
                            return;
                        }
                    }
                    else if (item.PrimaryItemAttribute == StorageItemTypes.Folder && !item.IsZipItem)
                    {
                        if (await StorageHelpers.ToStorageItem<BaseStorageFolder>(item.ItemPath) is BaseStorageFolder folder)
                        {
                            items.Add(folder);
                        }
                    }
                    else
                    {
                        if (await StorageHelpers.ToStorageItem<BaseStorageFile>(item.ItemPath) is BaseStorageFile file)
                        {
                            items.Add(file);
                        }
                    }
                }

                if (items.Count == 1)
                {
                    dataRequest.Data.Properties.Title = string.Format("ShareDialogTitle".GetLocalizedResource(), items.First().Name);
                    dataRequest.Data.Properties.Description = "ShareDialogSingleItemDescription".GetLocalizedResource();
                }
                else if (items.Count == 0)
                {
                    dataRequest.FailWithDisplayText("ShareDialogFailMessage".GetLocalizedResource());
                    dataRequestDeferral.Complete();
                    return;
                }
                else
                {
                    dataRequest.Data.Properties.Title = string.Format("ShareDialogTitleMultipleItems".GetLocalizedResource(), items.Count,
                        "ItemsCount.Text".GetLocalizedResource());
                    dataRequest.Data.Properties.Description = "ShareDialogMultipleItemsDescription".GetLocalizedResource();
                }

                dataRequest.Data.SetStorageItems(items, false);
                dataRequestDeferral.Complete();

                // TODO: Unhook the event somewhere
            }
        }

        public virtual void PinDirectoryToFavorites()
        {
            App.SidebarPinnedController.Model.AddItem(associatedViewModel.FilesystemViewModel.WorkingDirectory);
        }

        public virtual async void ItemPointerPressed(Pointer)
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

        public virtual async void UnpinItemFromStart()
        {
            if (associatedViewModel.SlimContentPage.SelectedItems.Count > 0)
            {
                foreach (ListedItem listedItem in associatedViewModel.SlimContentPage.SelectedItems)
                {
                    await App.SecondaryTileHelper.UnpinFromStartAsync(listedItem.ItemPath);
                }
            }
            else
            {
                await App.SecondaryTileHelper.UnpinFromStartAsync(associatedViewModel.FilesystemViewModel.WorkingDirectory);
            }
        }

        public async void PinItemToStart()
        {
            if (associatedViewModel.SlimContentPage.SelectedItems.Count > 0)
            {
                foreach (ListedItem listedItem in associatedViewModel.SlimContentPage.SelectedItems)
                {
                    await App.SecondaryTileHelper.TryPinFolderAsync(listedItem.ItemPath, listedItem.ItemName);
                }
            }
            else
            {
                await App.SecondaryTileHelper.TryPinFolderAsync(associatedViewModel.FilesystemViewModel.CurrentFolder.ItemPath, associatedViewModel.FilesystemViewModel.CurrentFolder.ItemName);
            }
        }

        public virtual void PointerWheelChanged(Pointer)
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
            if (associatedViewModel.IsCurrentInstance)
            {
                associatedViewModel.InstanceViewModel.FolderSettings.GridViewSize = associatedViewModel.InstanceViewModel.FolderSettings.GridViewSize - Constants.Browser.GridViewBrowser.GridViewIncrement; // Make Smaller
            }
            if (e != null)
            {
                e.Handled = true;
            }
        }

        public virtual void GridViewSizeIncrease(KeyboardAcceleratorInvokedEventArgs e)
        {
            if (associatedViewModel.IsCurrentInstance)
            {
                associatedViewModel.InstanceViewModel.FolderSettings.GridViewSize = associatedViewModel.InstanceViewModel.FolderSettings.GridViewSize + Constants.Browser.GridViewBrowser.GridViewIncrement; // Make Larger
            }
            if (e != null)
            {
                e.Handled = true;
            }
        }

        public virtual async Task DragOver(DragEventArgs e)
        {
            var deferral = e.GetDeferral();

            if (associatedViewModel.InstanceViewModel.IsPageTypeSearchResults)
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

                var pwd = associatedViewModel.FilesystemViewModel.WorkingDirectory.TrimPath();
                var folderName = (Path.IsPathRooted(pwd) && Path.GetPathRoot(pwd) == pwd) ? Path.GetPathRoot(pwd) : Path.GetFileName(pwd);

                // As long as one file doesn't already belong to this folder
                if (associatedViewModel.InstanceViewModel.IsPageTypeSearchResults || (draggedItems.Any() && draggedItems.AreItemsAlreadyInFolder(associatedViewModel.FilesystemViewModel.WorkingDirectory)))
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
                        e.DragUIOverride.Caption = string.Format("CopyToFolderCaptionText".GetLocalizedResource(), folderName);
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
                        e.DragUIOverride.Caption = string.Format("MoveToFolderCaptionText".GetLocalizedResource(), folderName);
                        e.AcceptedOperation = DataPackageOperation.Move;
                    }
                    else if (e.Modifiers.HasFlag(DragDropModifiers.Alt) || e.Modifiers.HasFlag(DragDropModifiers.Control | DragDropModifiers.Shift))
                    {
                        e.DragUIOverride.Caption = string.Format("LinkToFolderCaptionText".GetLocalizedResource(), folderName);
                        e.AcceptedOperation = DataPackageOperation.Link;
                    }
                    else if (e.Modifiers.HasFlag(DragDropModifiers.Control))
                    {
                        e.DragUIOverride.Caption = string.Format("CopyToFolderCaptionText".GetLocalizedResource(), folderName);
                        e.AcceptedOperation = DataPackageOperation.Copy;
                    }
                    else if (e.Modifiers.HasFlag(DragDropModifiers.Shift))
                    {
                        e.DragUIOverride.Caption = string.Format("MoveToFolderCaptionText".GetLocalizedResource(), folderName);
                        e.AcceptedOperation = DataPackageOperation.Move;
                    }
                    else if (draggedItems.Any(x => x.Item is ZipStorageFile || x.Item is ZipStorageFolder)
                        || ZipStorageFolder.IsZipPath(pwd))
                    {
                        e.DragUIOverride.Caption = string.Format("CopyToFolderCaptionText".GetLocalizedResource(), folderName);
                        e.AcceptedOperation = DataPackageOperation.Copy;
                    }
                    else if (draggedItems.AreItemsInSameDrive(associatedViewModel.FilesystemViewModel.WorkingDirectory))
                    {
                        e.DragUIOverride.Caption = string.Format("MoveToFolderCaptionText".GetLocalizedResource(), folderName);
                        e.AcceptedOperation = DataPackageOperation.Move;
                    }
                    else
                    {
                        e.DragUIOverride.Caption = string.Format("CopyToFolderCaptionText".GetLocalizedResource(), folderName);
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
                await associatedViewModel.FilesystemHelpers.PerformOperationTypeAsync(e.AcceptedOperation, e.DataView, associatedViewModel.FilesystemViewModel.WorkingDirectory, false, true);
                e.Handled = true;
            }

            deferral.Complete();
        }

        public virtual void RefreshItems()
        {
            associatedViewModel.Refresh_Click();
        }

        public void SearchUnindexedItems()
        {
            associatedViewModel.SubmitSearch(associatedViewModel.InstanceViewModel.CurrentSearchQuery, true);
        }

        public async Task CreateFolderWithSelection()
        {
            await UIFilesystemHelpers.CreateFolderWithSelectionAsync(associatedViewModel);
        }

        public async Task DecompressArchive()
        {
            BaseStorageFile archive = await StorageHelpers.ToStorageItem<BaseStorageFile>(associatedViewModel.SlimContentPage.SelectedItem.ItemPath);

            if (archive == null)
                return;

            DecompressArchiveDialog decompressArchiveDialog = new();
            DecompressArchiveDialogViewModel decompressArchiveViewModel = new(archive);
            decompressArchiveDialog.ViewModel = decompressArchiveViewModel;

            ContentDialogResult option = await decompressArchiveDialog.ShowAsync();

            if (option != ContentDialogResult.Primary)
                return;

            // Check if archive still exists
            if (!StorageHelpers.Exists(archive.Path))
                return;

            BaseStorageFolder destinationFolder = decompressArchiveViewModel.DestinationFolder;
            string destinationFolderPath = decompressArchiveViewModel.DestinationFolderPath;

            if (destinationFolder == null)
            {
                BaseStorageFolder parentFolder = await StorageHelpers.ToStorageItem<BaseStorageFolder>(Path.GetDirectoryName(archive.Path));
                destinationFolder = await FilesystemTasks.Wrap(() => parentFolder.CreateFolderAsync(Path.GetFileName(destinationFolderPath), CreationCollisionOption.GenerateUniqueName).AsTask());
            }

            await ExtractArchive(archive, destinationFolder);

            if (decompressArchiveViewModel.OpenDestinationFolderOnCompletion)
                await NavigationHelpers.OpenPath(destinationFolderPath, associatedViewModel, FilesystemItemType.Directory);
        }

        public async Task DecompressArchiveHere()
        {
            foreach (var selectedItem in associatedViewModel.SlimContentPage.SelectedItems)
            {
                BaseStorageFile archive = await StorageHelpers.ToStorageItem<BaseStorageFile>(selectedItem.ItemPath);
                BaseStorageFolder currentFolder = await StorageHelpers.ToStorageItem<BaseStorageFolder>(associatedViewModel.FilesystemViewModel.CurrentFolder.ItemPath);

                await ExtractArchive(archive, currentFolder);
            }
        }

        public async Task DecompressArchiveToChildFolder()
        {
            foreach (var selectedItem in associatedViewModel.SlimContentPage.SelectedItems)
            {
                BaseStorageFile archive = await StorageHelpers.ToStorageItem<BaseStorageFile>(selectedItem.ItemPath);
                BaseStorageFolder currentFolder = await StorageHelpers.ToStorageItem<BaseStorageFolder>(associatedViewModel.FilesystemViewModel.CurrentFolder.ItemPath);
                BaseStorageFolder destinationFolder = null;

                if (currentFolder != null)
                    destinationFolder = await FilesystemTasks.Wrap(() => currentFolder.CreateFolderAsync(Path.GetFileNameWithoutExtension(archive.Path), CreationCollisionOption.GenerateUniqueName).AsTask());

                await ExtractArchive(archive, destinationFolder);
            }
        }

        private static async Task ExtractArchive(BaseStorageFile archive, BaseStorageFolder destinationFolder)
        {
            if (archive == null || destinationFolder == null)
                return;

            CancellationTokenSource extractCancellation = new();
            PostedStatusBanner banner = App.OngoingTasksViewModel.PostOperationBanner(
                archive.Name.Length >= 30 ? archive.Name + "\n" : archive.Name,
                "ExtractingArchiveText".GetLocalizedResource(),
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
                    "ExtractingCompleteText".GetLocalizedResource(),
                    "ArchiveExtractionCompletedSuccessfullyText".GetLocalizedResource(),
                    0,
                    ReturnResult.Success,
                    FileOperationType.Extract);
            }
        }

        public async Task InstallInfDriver()
        {
            foreach (ListedItem selectedItem in SlimContentPage.SelectedItems)
            {
                await Win32API.InstallInf(selectedItem.ItemPath);
            }
        }

        public async Task RotateImageLeft()
        {
            foreach (var image in SlimContentPage.SelectedItems)
            {
                await BitmapHelper.Rotate(PathNormalization.NormalizePath(image.ItemPath), BitmapRotation.Clockwise270Degrees);
            }

            SlimContentPage.ItemManipulationModel.RefreshItemsThumbnail();
            App.PreviewPaneViewModel.UpdateSelectedItemPreview();
        }

        public async Task RotateImageRight()
        {
            foreach (var image in SlimContentPage.SelectedItems)
            {
                await BitmapHelper.Rotate(PathNormalization.NormalizePath(image.ItemPath), BitmapRotation.Clockwise90Degrees);
            }

            SlimContentPage.ItemManipulationModel.RefreshItemsThumbnail();
            App.PreviewPaneViewModel.UpdateSelectedItemPreview();
        }

        public Task InstallFont()
        {
            foreach (ListedItem selectedItem in SlimContentPage.SelectedItems)
            {
                Win32API.InstallFont(selectedItem.ItemPath);
            }

            return Task.CompletedTask;
        }
    }
}