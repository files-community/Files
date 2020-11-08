using Files.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Files.Dialogs;
using Windows.Foundation.Collections;
using Windows.ApplicationModel.AppService;
using Files.Filesystem.FilesystemHistory;
using Microsoft.Toolkit.Uwp.Extensions;
using FileAttributes = System.IO.FileAttributes;
using static Files.Dialogs.ConfirmDeleteDialog;
using static Files.Helpers.NativeFindStorageItemHelper;

namespace Files.Filesystem
{
    public enum ImpossibleActionResponseTypes
    {
        Skip = 0,
        Abort = 1
    }

    public class FilesystemOperations : IFilesystemOperations
    {
        #region Private Members

        private readonly IShellPage _associatedInstance;

        #endregion

        #region Constructor

        public FilesystemOperations(IShellPage associatedInstance)
        {
            this._associatedInstance = associatedInstance;
        }

        #endregion

        #region IFilesystemOperations

        #region Copy, Move

        public async Task<IStorageHistory> CopyAsync(IStorageItem source, IStorageItem destination, IProgress<float> progress, IProgress<Status> status, CancellationToken cancellationToken)
        {
            if (_associatedInstance.FilesystemViewModel.WorkingDirectory.StartsWith(App.AppSettings.RecycleBinPath))
            {
                // Do not paste files and folders inside the recycle bin
                await DialogDisplayHelper.ShowDialog("ErrorDialogThisActionCannotBeDone".GetLocalized(), "ErrorDialogUnsupportedOperation".GetLocalized());

                status?.Report(Status.Failed | Status.IllegalArgumentException);
                return null;
            }

            List<IStorageItem> pastedSourceItems = new List<IStorageItem>();
            HashSet<IStorageItem> pastedItems = new HashSet<IStorageItem>();
            long totalItemsSize = CalculateTotalItemsSize(new List<IStorageItem>() { source });
            bool isItemSizeUnreported = true;

            progress?.Report(0.0f);
            if (source.IsOfType(StorageItemTypes.Folder))
            {
                if (!string.IsNullOrEmpty(source.Path) && destination.Path.IsSubPathOf(source.Path))
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
                        status?.Report(Status.Cancelled);
                        return null;
                    }
                    else if (responseType == ImpossibleActionResponseTypes.Abort)
                    {
                        status?.Report(Status.Failed | Status.Cancelled);
                        return null;
                    }
                }
                else
                {
                    if (!isItemSizeUnreported)
                    {
                        long pastedItemSize = await Task.Run(() => CalculateTotalItemsSize(pastedSourceItems));
                        float progressValue = (float)(pastedItemSize * 100.0f / totalItemsSize);
                        progress?.Report(progressValue);
                    }

                    try
                    {
                        StorageFolder pastedOutput = await FilesystemHelpers.CloneDirectoryAsync(
                            (StorageFolder)source,
                            await _associatedInstance.FilesystemViewModel.GetFolderFromPathAsync(destination.Path),
                            source.Name);
                        pastedSourceItems.Add(source);
                        pastedItems.Add(pastedOutput);
                    }
                    catch (FileNotFoundException)
                    {
                        // Folder was moved/deleted in the meantime
                        status?.Report(Status.Failed | Status.IntegrityCheckFailed);
                        return null;
                    }
                }
            }
            else if (source.IsOfType(StorageItemTypes.File))
            {
                if (!isItemSizeUnreported)
                {
                    long pastedItemSize = await Task.Run(() => CalculateTotalItemsSize(pastedSourceItems));
                    float progressValue = (uint)(pastedItemSize * 100.0f / totalItemsSize);
                    progress?.Report(progressValue);
                }

                try
                {
                    StorageFile clipboardFile = (StorageFile)source;
                    StorageFile pastedFile = await clipboardFile.CopyAsync(
                        await _associatedInstance.FilesystemViewModel.GetFolderFromPathAsync(destination.Path),
                        source.Name,
                        NameCollisionOption.GenerateUniqueName);
                    pastedSourceItems.Add(source);
                    pastedItems.Add(pastedFile);
                }
                catch (UnauthorizedAccessException)
                {
                    status?.Report(Status.AccessUnauthorized);

                    // Try again with CopyFileFromApp
                    if (NativeDirectoryChangesHelper.CopyFileFromApp(source.Path, Path.Combine(destination.Path, source.Name), true))
                    {
                        pastedSourceItems.Add(source);
                    }
                    else
                    {
                        status?.Report(Status.Failed | Status.AccessUnauthorized);
                        Debug.WriteLine(System.Runtime.InteropServices.Marshal.GetLastWin32Error());
                    }
                }
                catch (FileNotFoundException)
                {
                    // File was moved/deleted in the meantime
                    status?.Report(Status.Failed | Status.IntegrityCheckFailed);
                    return null;
                }
            }

            if (!isItemSizeUnreported)
            {
                long finalPastedItemSize = await Task.Run(() => CalculateTotalItemsSize(pastedSourceItems));
                float finalProgressValue = (uint)(finalPastedItemSize * 100.0f / totalItemsSize);
                progress?.Report(finalProgressValue);
            }
            else
                progress?.Report(100.0f);

            if (destination.Path == _associatedInstance.FilesystemViewModel.WorkingDirectory)
            {
                List<string> pastedItemPaths = pastedItems.Select(item => item.Path).ToList();
                List<ListedItem> copiedItems = _associatedInstance.FilesystemViewModel.FilesAndFolders.Where(listedItem => pastedItemPaths.Contains(listedItem.ItemPath)).ToList();
                if (copiedItems.Any())
                {
                    _associatedInstance.ContentPage.SetSelectedItemsOnUi(copiedItems);
                    _associatedInstance.ContentPage.FocusSelectedItems();
                }
            }

            status?.Report(Status.Success);

            return new StorageHistory(FileOperationType.Copy, new List<IStorageItem>() { source }, new List<IStorageItem>() { destination });
        }

        public async Task<IStorageHistory> MoveAsync(IStorageItem source, IStorageItem destination, IProgress<float> progress, IProgress<Status> status, CancellationToken cancellationToken)
        {
            await CopyAsync(source, destination, progress, status, cancellationToken);

            try
            {
                if (string.IsNullOrEmpty(source.Path))
                {
                    // Can't move (only copy) files from MTP devices because:
                    // StorageItems returned in DataPackageView are read-only
                    // The item.Path property will be empty and there's no way of retrieving a new StorageItem with R/W access

                    status?.Report(Status.Failed | Status.IllegalArgumentException);
                    return null;
                }
                if (source.IsOfType(StorageItemTypes.File))
                {
                    // If we reached this we are not in an MTP device, using StorageFile.* is ok here
                    StorageFile file = await StorageFile.GetFileFromPathAsync(source.Path);
                    await file.DeleteAsync(StorageDeleteOption.PermanentDelete);
                }
                else if (source.IsOfType(StorageItemTypes.Folder))
                {
                    // If we reached this we are not in an MTP device, using StorageFolder.* is ok here
                    StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(source.Path);
                    await folder.DeleteAsync(StorageDeleteOption.PermanentDelete);
                }
            }
            catch (UnauthorizedAccessException)
            {
                status?.Report(Status.AccessUnauthorized);
                // Try again with DeleteFileFromApp
                if (!NativeDirectoryChangesHelper.DeleteFileFromApp(source.Path))
                {
                    status?.Report(Status.Failed | Status.AccessUnauthorized);
                    Debug.WriteLine(System.Runtime.InteropServices.Marshal.GetLastWin32Error());
                    return null;
                }
            }
            catch (FileNotFoundException)
            {
                // File or Folder was moved/deleted in the meantime
                status?.Report(Status.IntegrityCheckFailed);
                return null;
            }
            ListedItem listedItem = _associatedInstance.FilesystemViewModel.FilesAndFolders.FirstOrDefault(listedItem => listedItem.ItemPath.Equals(source.Path, StringComparison.OrdinalIgnoreCase));

            progress?.Report(100.0f);
            status?.Report(Status.Success);

            return new StorageHistory(FileOperationType.Move, new List<IStorageItem>() { source }, new List<IStorageItem>() { destination });
        }

        #endregion

        public async Task<IStorageHistory> CreateAsync(string fullPath, FilesystemItemType itemType, IProgress<Status> status, CancellationToken cancellationToken)
        {
            switch (itemType)
            {
                case FilesystemItemType.File:
                    {
                        StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(Path.GetDirectoryName(fullPath));
                        await folder.CreateFileAsync(Path.GetFileName(fullPath));

                        break;
                    }

                case FilesystemItemType.Directory:
                    {
                        StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(Path.GetDirectoryName(fullPath));
                        await folder.CreateFolderAsync(Path.GetFileName(fullPath));

                        break;
                    }

                case FilesystemItemType.Symlink:
                    {
                        Debugger.Break();
                        throw new NotImplementedException();
                    }

                default:
                    Debugger.Break();
                    break;
            }

            return new StorageHistory(FileOperationType.CreateNew, new List<IStorageItem>() { await fullPath.ToStorageItem() }, null);
        }

        public async Task<IStorageHistory> RenameAsync(IStorageItem source, string newName, bool replace, IProgress<Status> status, CancellationToken cancellationToken)
        {
            string originalSource = source.Path;
            await source.RenameAsync(newName, replace ? NameCollisionOption.ReplaceExisting : NameCollisionOption.GenerateUniqueName);

            return new StorageHistory(FileOperationType.Rename, new List<string>() { originalSource }, new List<string>() { source.Path });
        }

        #region Trash/Delete

        public async Task<IStorageHistory> DeleteAsync(IStorageItem source, IProgress<float> progress, IProgress<Status> status, bool permanently, CancellationToken cancellationToken)
        {
            bool deleteFromRecycleBin = _associatedInstance.FilesystemViewModel.WorkingDirectory.StartsWith(App.AppSettings.RecycleBinPath);

            IStorageItem item;
            try
            {
                if (source.IsOfType(StorageItemTypes.File))
                {
                    item = await _associatedInstance.FilesystemViewModel.GetFileFromPathAsync(source.Path);
                }
                else
                {
                    item = await _associatedInstance.FilesystemViewModel.GetFolderFromPathAsync(source.Path);
                }

                await item.DeleteAsync(permanently ? StorageDeleteOption.PermanentDelete : StorageDeleteOption.Default);
            }
            catch (UnauthorizedAccessException)
            {
                if (!permanently)
                {
                    // Try again with fulltrust process
                    if (_associatedInstance.FilesystemViewModel.Connection != null)
                    {
                        AppServiceResponse result = await _associatedInstance.FilesystemViewModel.Connection.SendMessageAsync(new ValueSet() {
                            { "Arguments", "FileOperation" },
                            { "fileop", "MoveToBin" },
                            { "filepath", source.Path } });
                    }
                }
                else
                {
                    // Try again with DeleteFileFromApp
                    if (!NativeDirectoryChangesHelper.DeleteFileFromApp(source.Path))
                    {
                        Debug.WriteLine(System.Runtime.InteropServices.Marshal.GetLastWin32Error());
                    }
                }
            }
            catch (FileLoadException)
            {
                // try again
                if (source.IsOfType(StorageItemTypes.File))
                {
                    item = await _associatedInstance.FilesystemViewModel.GetFileFromPathAsync(source.Path);
                }
                else
                {
                    item = await _associatedInstance.FilesystemViewModel.GetFolderFromPathAsync(source.Path);
                }

                await item.DeleteAsync(permanently ? StorageDeleteOption.PermanentDelete : StorageDeleteOption.Default);
            }

            if (deleteFromRecycleBin)
            {
                // Recycle bin also stores a file starting with $I for each item
                string iFilePath = Path.Combine(Path.GetDirectoryName(source.Path), Path.GetFileName(source.Path).Replace("$R", "$I"));
                await (await _associatedInstance.FilesystemViewModel.GetFileFromPathAsync(iFilePath)).DeleteAsync(StorageDeleteOption.PermanentDelete);
            }

            await _associatedInstance.FilesystemViewModel.RemoveFileOrFolder(source.Path);

            status?.Report(Status.Success);
            return new StorageHistory(permanently ? FileOperationType.Delete : FileOperationType.Recycle,
                new List<IStorageItem>() { source }, null); // TODO: Return destination here as RecycleBinItem?
        }

        public async Task<IStorageHistory> RestoreFromTrashAsync(RecycleBinItem source, IStorageItem destination, IProgress<float> progress, IProgress<Status> status, CancellationToken cancellationToken)
        {
            try
            {
                if (source.PrimaryItemAttribute == StorageItemTypes.Folder)
                {
                    StorageFolder sourceFolder = await _associatedInstance.FilesystemViewModel.GetFolderFromPathAsync(source.ItemPath);
                    await MoveAsync(sourceFolder, destination, progress, status, cancellationToken);
                    await sourceFolder.DeleteAsync(StorageDeleteOption.PermanentDelete);
                }
                else if (source.PrimaryItemAttribute == StorageItemTypes.File)
                {
                    StorageFile file = await _associatedInstance.FilesystemViewModel.GetFileFromPathAsync(source.ItemPath);
                    await file.MoveAsync((IStorageFolder)destination, Path.GetFileName(source.ItemOriginalPath), NameCollisionOption.GenerateUniqueName);
                }

                // Recycle bin also stores a file starting with $I for each item
                string iFilePath = Path.Combine(Path.GetDirectoryName(source.ItemPath), Path.GetFileName(source.ItemPath).Replace("$R", "$I"));
                await (await _associatedInstance.FilesystemViewModel.GetFileFromPathAsync(iFilePath)).DeleteAsync(StorageDeleteOption.PermanentDelete);

                status?.Report(Status.Success);
                return new StorageHistory(FileOperationType.Restore, new List<RecycleBinItem>() { source }, new List<IStorageItem>() { destination });
            }
            catch (UnauthorizedAccessException)
            {
                status?.Report(Status.AccessUnauthorized);
                await DialogDisplayHelper.ShowDialog("AccessDeniedDeleteDialog/Title".GetLocalized(), "AccessDeniedDeleteDialog/Text".GetLocalized());
                return null;
            }
            catch (FileNotFoundException)
            {
                status?.Report(Status.IntegrityCheckFailed);
                await DialogDisplayHelper.ShowDialog("FileNotFoundDialog/Title".GetLocalized(), "FileNotFoundDialog/Text".GetLocalized());
                return null;
            }
            catch (Exception)
            {
                status?.Report(Status.UnknownException);
                await DialogDisplayHelper.ShowDialog("ItemAlreadyExistsDialogTitle".GetLocalized(), "ItemAlreadyExistsDialogContent".GetLocalized());
                return null;
            }
        }

        #endregion

        #endregion

        public static long CalculateTotalItemsSize(IEnumerable<IStorageItem> itemsToPaste)
        {
            var folderSizes = itemsToPaste.Where(item => item.IsOfType(StorageItemTypes.Folder)).Select(async item => await CalculateFolderSizeAsync(item.Path)).Select(item => item.Result);
            var fileSizes = itemsToPaste.Where(item => item.IsOfType(StorageItemTypes.File)).Select(item => CalculateFileSize(item));
            return folderSizes.Sum() + fileSizes.Sum();
        }

        public static async Task<long> CalculateFolderSizeAsync(string folder)
        {
            if (string.IsNullOrEmpty(folder))
            {
                // In MTP devices calculating folder size would be too slow
                // Also should use StorageFolder methods instead of FindFirstFileExFromApp
                return 0;
            }

            long size = 0;
            FINDEX_INFO_LEVELS findInfoLevel = FINDEX_INFO_LEVELS.FindExInfoBasic;
            int additionalFlags = FIND_FIRST_EX_LARGE_FETCH;

            IntPtr hFile = FindFirstFileExFromApp(folder + "\\*.*", findInfoLevel, out WIN32_FIND_DATA findData, FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero,
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
                            var itemPath = Path.Combine(folder, findData.cFileName);

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

        public static long CalculateFileSize(IStorageItem file)
        {
            if (string.IsNullOrEmpty(file.Path))
            {
                // In MTP devices calculating folder size would be too slow
                // Also should use StorageFolder methods instead of FindFirstFileExFromApp
                return 0;
            }

            long size = 0;
            FINDEX_INFO_LEVELS findInfoLevel = FINDEX_INFO_LEVELS.FindExInfoBasic;
            int additionalFlags = FIND_FIRST_EX_LARGE_FETCH;

            IntPtr hFile = FindFirstFileExFromApp(file.Path, findInfoLevel, out WIN32_FIND_DATA findData, FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero,
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
