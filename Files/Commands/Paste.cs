using Files.Filesystem;
using Files.Helpers;
using Files.UserControls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace Files.Commands
{
    public partial class ItemOperations
    {
        private enum ImpossibleActionResponseTypes
        {
            Skip,
            Abort
        }

        public static async void PasteItemWithStatus(DataPackageView packageView, string destinationPath, DataPackageOperation acceptedOperation)
        {
            var CurrentInstance = App.CurrentInstance;
            PostedStatusBanner banner = App.CurrentInstance.StatusBarControl.OngoingTasksControl.PostBanner(
                null,
                CurrentInstance.FilesystemViewModel.WorkingDirectory,
                0,
                StatusBanner.StatusBannerSeverity.Ongoing,
                StatusBanner.StatusBannerOperation.Paste);

            Stopwatch sw = new Stopwatch();
            sw.Start();

            await PasteItem(packageView, destinationPath, acceptedOperation, CurrentInstance, banner.Progress);
            banner.Remove();

            sw.Stop();

            if (sw.Elapsed.TotalSeconds >= 10)
            {
                App.CurrentInstance.StatusBarControl.OngoingTasksControl.PostBanner(
                    "Paste Complete",
                    "The operation has completed.",
                    0,
                    StatusBanner.StatusBannerSeverity.Success,
                    StatusBanner.StatusBannerOperation.Paste);
            }
        }

        private static async Task PasteItem(DataPackageView packageView, string destinationPath, DataPackageOperation acceptedOperation, IShellPage AppInstance, IProgress<uint> progress)
        {
            int itemsPasted = 0;
            IReadOnlyList<IStorageItem> itemsToPaste = await packageView.GetStorageItemsAsync();

            if (!packageView.Contains(StandardDataFormats.StorageItems))
            {
                // Happens if you copy some text and then you Ctrl+V in FilesUWP
                // Should this be done in ModernShellPage?
                return;
            }
            if (AppInstance.FilesystemViewModel.WorkingDirectory.StartsWith(App.AppSettings.RecycleBinPath))
            {
                // Do not paste files and folders inside the recycle bin
                await DialogDisplayHelper.ShowDialog(ResourceController.GetTranslation("ErrorDialogThisActionCannotBeDone"), ResourceController.GetTranslation("ErrorDialogUnsupportedOperation"));
                return;
            }
            
            HashSet<IStorageItem> pastedSourceItems = new HashSet<IStorageItem>();
            HashSet<IStorageItem> pastedItems = new HashSet<IStorageItem>();

            foreach (IStorageItem item in itemsToPaste)
            {
                if (item.IsOfType(StorageItemTypes.Folder))
                {
                    if (!string.IsNullOrEmpty(item.Path) && destinationPath.IsSubPathOf(item.Path))
                    {
                        ImpossibleActionResponseTypes responseType = ImpossibleActionResponseTypes.Abort;

                        /// Currently following implementation throws exception until it is resolved keep it disabled
                        /*Binding themeBind = new Binding();
                        themeBind.Source = ThemeHelper.RootTheme;

                        ContentDialog dialog = new ContentDialog()
                        {
                            Title = ResourceController.GetTranslation("ErrorDialogThisActionCannotBeDone"),
                            Content = ResourceController.GetTranslation("ErrorDialogTheDestinationFolder") + " (" + destinationPath.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries).Last() + ") " + ResourceController.GetTranslation("ErrorDialogIsASubfolder") + " (" + item.Name + ")",
                            PrimaryButtonText = ResourceController.GetTranslation("ErrorDialogSkip"),
                            CloseButtonText = ResourceController.GetTranslation("ErrorDialogCancel"),
                            PrimaryButtonCommand = new RelayCommand(() => { responseType = ImpossibleActionResponseTypes.Skip; }),
                            CloseButtonCommand = new RelayCommand(() => { responseType = ImpossibleActionResponseTypes.Abort; }),
                        };
                        BindingOperations.SetBinding(dialog, FrameworkElement.RequestedThemeProperty, themeBind);

                        await dialog.ShowAsync();*/
                        if (responseType == ImpossibleActionResponseTypes.Skip)
                        {
                            continue;
                        }
                        else if (responseType == ImpossibleActionResponseTypes.Abort)
                        {
                            return;
                        }
                    }
                    else
                    {
                        try
                        {
                            ClonedDirectoryOutput pastedOutput = await CloneDirectoryAsync(
                                (StorageFolder)item,
                                await ItemViewModel.GetFolderFromPathAsync(destinationPath),
                                item.Name, 
                                itemsToPaste, 
                                itemsPasted, 
                                progress);
                            pastedSourceItems.Add(item);
                            pastedItems.Add(pastedOutput.FolderOutput);
                            itemsPasted = pastedOutput.PastedItemCount;
                        }
                        catch (FileNotFoundException)
                        {
                            // Folder was moved/deleted in the meantime
                            continue;
                        }
                    }
                }
                else if (item.IsOfType(StorageItemTypes.File))
                {
                    uint progressValue = (uint)(itemsPasted * 100.0 / itemsToPaste.Count);
                    progress.Report((uint)progressValue);

                    try
                    {
                        StorageFile clipboardFile = (StorageFile)item;
                        StorageFile pastedFile = await clipboardFile.CopyAsync(
                            await ItemViewModel.GetFolderFromPathAsync(destinationPath),
                            item.Name,
                            NameCollisionOption.GenerateUniqueName);
                        pastedSourceItems.Add(item);
                        pastedItems.Add(pastedFile);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // Try again with CopyFileFromApp
                        if (NativeDirectoryChangesHelper.CopyFileFromApp(item.Path, Path.Combine(destinationPath, item.Name), true))
                        {
                            pastedSourceItems.Add(item);
                        }
                        else
                        {
                            Debug.WriteLine(System.Runtime.InteropServices.Marshal.GetLastWin32Error());
                        }
                    }
                    catch (FileNotFoundException)
                    {
                        // File was moved/deleted in the meantime
                        continue;
                    }
                }
                itemsPasted++;
            }

            if (acceptedOperation == DataPackageOperation.Move)
            {
                foreach (IStorageItem item in pastedSourceItems)
                {
                    try
                    {
                        if (string.IsNullOrEmpty(item.Path))
                        {
                            // Can't move (only copy) files from MTP devices because:
                            // StorageItems returned in DataPackageView are read-only
                            // The item.Path property will be empty and there's no way of retrieving a new StorageItem with R/W access
                            continue;
                        }
                        if (item.IsOfType(StorageItemTypes.File))
                        {
                            // If we reached this we are not in an MTP device, using StorageFile.* is ok here
                            StorageFile file = await StorageFile.GetFileFromPathAsync(item.Path);
                            await file.DeleteAsync(StorageDeleteOption.PermanentDelete);
                        }
                        else if (item.IsOfType(StorageItemTypes.Folder))
                        {
                            // If we reached this we are not in an MTP device, using StorageFolder.* is ok here
                            StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(item.Path);
                            await folder.DeleteAsync(StorageDeleteOption.PermanentDelete);
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // Try again with DeleteFileFromApp
                        if (!NativeDirectoryChangesHelper.DeleteFileFromApp(item.Path))
                        {
                            Debug.WriteLine(System.Runtime.InteropServices.Marshal.GetLastWin32Error());
                        }
                    }
                    catch (FileNotFoundException)
                    {
                        // File or Folder was moved/deleted in the meantime
                        continue;
                    }
                    ListedItem listedItem = AppInstance.FilesystemViewModel.FilesAndFolders.FirstOrDefault(listedItem => listedItem.ItemPath.Equals(item.Path, StringComparison.OrdinalIgnoreCase));
                }
            }

            if (destinationPath == AppInstance.FilesystemViewModel.WorkingDirectory)
            {
                List<string> pastedItemPaths = pastedItems.Select(item => item.Path).ToList();
                List<ListedItem> copiedItems = AppInstance.FilesystemViewModel.FilesAndFolders.Where(listedItem => pastedItemPaths.Contains(listedItem.ItemPath)).ToList();
                if (copiedItems.Any())
                {
                    AppInstance.ContentPage.SetSelectedItemsOnUi(copiedItems);
                    AppInstance.ContentPage.FocusSelectedItems();
                }
            }
            packageView.ReportOperationCompleted(acceptedOperation);
        }

        public class ClonedDirectoryOutput
        {
            public StorageFolder FolderOutput { get; set; }
            public int PastedItemCount { get; set; }
        }

        public async static Task<ClonedDirectoryOutput> CloneDirectoryAsync(StorageFolder SourceFolder, StorageFolder DestinationFolder, string sourceRootName, IReadOnlyList<IStorageItem> itemsToPaste, int currentPastedCount, IProgress<uint> progress)
        {
            var createdRoot = await DestinationFolder.CreateFolderAsync(sourceRootName, CreationCollisionOption.GenerateUniqueName);
            DestinationFolder = createdRoot;

            foreach (StorageFile fileInSourceDir in await SourceFolder.GetFilesAsync())
            {
                if (itemsToPaste != null)
                {
                    uint progressValue = (uint)(currentPastedCount * 100.0 / (itemsToPaste.Count + (await SourceFolder.GetFilesAsync()).Count));
                    progress.Report(progressValue);
                }

                await fileInSourceDir.CopyAsync(DestinationFolder, fileInSourceDir.Name, NameCollisionOption.GenerateUniqueName);
            }

            foreach (StorageFolder folderinSourceDir in await SourceFolder.GetFoldersAsync())
            {
                if (itemsToPaste != null)
                {
                    uint progressValue = (uint)(currentPastedCount * 100.0 / (itemsToPaste.Count + (await SourceFolder.GetFoldersAsync()).Count));
                    progress.Report(progressValue);
                }

                await CloneDirectoryAsync(folderinSourceDir, DestinationFolder, folderinSourceDir.Name, itemsToPaste, currentPastedCount, progress);
            }

            return new ClonedDirectoryOutput()
            {
                FolderOutput = createdRoot,
                PastedItemCount = currentPastedCount
            };
        }
    }
}
