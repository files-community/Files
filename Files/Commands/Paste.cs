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
using static Files.Helpers.NativeFindStorageItemHelper;
using FileAttributes = System.IO.FileAttributes;

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

            List<IStorageItem> pastedSourceItems = new List<IStorageItem>();
            HashSet<IStorageItem> pastedItems = new HashSet<IStorageItem>();
            var totalItemsSize = CalculateTotalItemsSize(itemsToPaste);
            bool isItemSizeUnreported = totalItemsSize <= 0;

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
                        if (!isItemSizeUnreported)
                        {
                            var pastedItemSize = await Task.Run(() => CalculateTotalItemsSize(pastedSourceItems));
                            uint progressValue = (uint)(pastedItemSize * 100 / totalItemsSize);
                            progress.Report(progressValue);
                        }

                        try
                        {
                            ClonedDirectoryOutput pastedOutput = await CloneDirectoryAsync(
                                (StorageFolder)item,
                                await ItemViewModel.GetFolderFromPathAsync(destinationPath),
                                item.Name);
                            pastedSourceItems.Add(item);
                            pastedItems.Add(pastedOutput.FolderOutput);
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
                    if (!isItemSizeUnreported)
                    {
                        var pastedItemSize = await Task.Run(() => CalculateTotalItemsSize(pastedSourceItems));
                        uint progressValue = (uint)(pastedItemSize * 100 / totalItemsSize);
                        progress.Report(progressValue);
                    }

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
            }
            if (!isItemSizeUnreported)
            {
                var finalPastedItemSize = await Task.Run(() => CalculateTotalItemsSize(pastedSourceItems));
                uint finalProgressValue = (uint)(finalPastedItemSize * 100 / totalItemsSize);
                progress.Report(finalProgressValue);
            }
            else
            {
                progress.Report(100);
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
            // TODO: simplify/remove this class
        }

        public async static Task<ClonedDirectoryOutput> CloneDirectoryAsync(StorageFolder SourceFolder, StorageFolder DestinationFolder, string sourceRootName)
        {
            var createdRoot = await DestinationFolder.CreateFolderAsync(sourceRootName, CreationCollisionOption.GenerateUniqueName);
            DestinationFolder = createdRoot;

            foreach (StorageFile fileInSourceDir in await SourceFolder.GetFilesAsync())
            {
                await fileInSourceDir.CopyAsync(DestinationFolder, fileInSourceDir.Name, NameCollisionOption.GenerateUniqueName);
            }

            foreach (StorageFolder folderinSourceDir in await SourceFolder.GetFoldersAsync())
            {
                await CloneDirectoryAsync(folderinSourceDir, DestinationFolder, folderinSourceDir.Name);
            }

            return new ClonedDirectoryOutput()
            {
                FolderOutput = createdRoot,
            };
        }

        public static long CalculateTotalItemsSize(IReadOnlyList<IStorageItem> itemsPasting)
        {
            var folderSizes = itemsPasting.Where(x => x.IsOfType(StorageItemTypes.Folder)).Select(async x => await CalculateFolderSizeAsync(x.Path)).Select(x => x.Result);
            var fileSizes = itemsPasting.Where(x => x.IsOfType(StorageItemTypes.File)).Select(x => CalculateFileSize(x.Path));
            return folderSizes.Sum() + fileSizes.Sum();
        }

        public static async Task<long> CalculateFolderSizeAsync(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                // In MTP devices calculating folder size would be too slow
                // Also should use StorageFolder methods instead of FindFirstFileExFromApp
                return 0;
            }

            long size = 0;
            FINDEX_INFO_LEVELS findInfoLevel = FINDEX_INFO_LEVELS.FindExInfoBasic;
            int additionalFlags = FIND_FIRST_EX_LARGE_FETCH;

            IntPtr hFile = FindFirstFileExFromApp(path + "\\*.*", findInfoLevel, out WIN32_FIND_DATA findData, FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero,
                                                  additionalFlags);

            var count = 0;
            if (hFile.ToInt64() != -1)
            {
                do
                {
                    if (((FileAttributes)findData.dwFileAttributes & FileAttributes.Directory) != FileAttributes.Directory)
                    {
                        size += findData.GetSize();
                        ++count;
                    }
                    else if (((FileAttributes)findData.dwFileAttributes & FileAttributes.Directory) == FileAttributes.Directory)
                    {
                        if (findData.cFileName != "." && findData.cFileName != "..")
                        {
                            var itemPath = Path.Combine(path, findData.cFileName);

                            size += await CalculateFolderSizeAsync(itemPath);
                            ++count;
                        }
                    }
                } while (FindNextFile(hFile, out findData));
                FindClose(hFile);
                return size;
            }
            else
            {
                return 0;
            }
        }

        public static long CalculateFileSize(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                // In MTP devices calculating folder size would be too slow
                // Also should use StorageFolder methods instead of FindFirstFileExFromApp
                return 0;
            }

            long size = 0;
            FINDEX_INFO_LEVELS findInfoLevel = FINDEX_INFO_LEVELS.FindExInfoBasic;
            int additionalFlags = FIND_FIRST_EX_LARGE_FETCH;

            IntPtr hFile = FindFirstFileExFromApp(path, findInfoLevel, out WIN32_FIND_DATA findData, FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero,
                                                  additionalFlags);

            if (hFile.ToInt64() != -1)
            {
                if (((FileAttributes)findData.dwFileAttributes & FileAttributes.Directory) != FileAttributes.Directory)
                {
                    size += findData.GetSize();
                }
                FindClose(hFile);
                Debug.WriteLine("Individual file size for Progress UI will be reported as: " + size.ToString() + " bytes");
                return size;
            }
            else
            {
                return 0;
            }
        }
    }
}