using Files.Extensions;
using Files.Filesystem.FilesystemHistory;
using Files.Helpers;
using Microsoft.Toolkit.Uwp.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation.Collections;
using Windows.Storage;
using FileAttributes = System.IO.FileAttributes;
using static Files.Helpers.NativeFindStorageItemHelper;
using Files.Common;

namespace Files.Filesystem
{
    public enum ImpossibleActionResponseTypes
    {
        Skip,
        Abort
    }

    public class FilesystemOperations : IFilesystemOperations
    {
        #region Private Members

        private IShellPage _associatedInstance;

        private AppServiceConnection _connection => _associatedInstance?.ServiceConnection;

        #endregion

        #region Constructor

        public FilesystemOperations(IShellPage associatedInstance)
        {
            this._associatedInstance = associatedInstance;
        }

        #endregion

        #region IFilesystemOperations

        public async Task<IStorageHistory> CreateAsync(string fullPath, FilesystemItemType itemType, IProgress<FilesystemErrorCode> errorCode, CancellationToken cancellationToken)
        {
            try
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

                errorCode?.Report(FilesystemErrorCode.ERROR_SUCCESS);
                return new StorageHistory(FileOperationType.CreateNew, fullPath, itemType.ToString());
            }
            catch (Exception e)
            {
                errorCode?.Report(FilesystemTasks.GetErrorCode(e));
                return null;
            }
        }

        public async Task<IStorageHistory> CopyAsync(IStorageItem source, string destination, IProgress<float> progress, IProgress<FilesystemErrorCode> errorCode, CancellationToken cancellationToken)
        {
            if (_associatedInstance.FilesystemViewModel.WorkingDirectory.StartsWith(App.AppSettings.RecycleBinPath))
            {
                errorCode?.Report(FilesystemErrorCode.ERROR_UNAUTHORIZED);
                progress?.Report(100.0f);

                // Do not paste files and folders inside the recycle bin
                await DialogDisplayHelper.ShowDialogAsync("ErrorDialogThisActionCannotBeDone".GetLocalized(), "ErrorDialogUnsupportedOperation".GetLocalized());
                return null;
            }

            IStorageItem copiedItem = null;
            long itemSize = await GetItemSize(source);
            bool reportProgress = false; // TODO: The default value is false

            if (source.IsOfType(StorageItemTypes.Folder))
            {
                if (!string.IsNullOrWhiteSpace(source.Path) && destination.IsSubPathOf(source.Path)) // TODO: Investigate
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
                        progress?.Report(100.0f);
                        errorCode?.Report(FilesystemErrorCode.ERROR_SUCCESS | FilesystemErrorCode.ERROR_INPROGRESS);
                    }
                    else if (responseType == ImpossibleActionResponseTypes.Abort)
                    {
                        progress?.Report(100.0f);
                        errorCode?.Report(FilesystemErrorCode.ERROR_INPROGRESS | FilesystemErrorCode.ERROR_GENERIC);
                    }
                }
                else
                {
                    if (reportProgress)
                    {
                        progress?.Report((float)(itemSize * 100.0f / itemSize));
                    }

                    await _associatedInstance.FilesystemViewModel.GetFolderFromPathAsync(Path.GetDirectoryName(destination))
                        .OnSuccess(t => CloneDirectoryAsync((IStorageFolder)source, t, source.Name))
                        .OnSuccess(t =>
                        {
                            copiedItem = t;
                        });
                }
            }
            else if (source.IsOfType(StorageItemTypes.File))
            {
                if (reportProgress)
                {
                    progress?.Report((float)(itemSize * 100.0f / itemSize));
                }

                FilesystemResult<StorageFolder> fsResult = await _associatedInstance.FilesystemViewModel.GetFolderFromPathAsync(Path.GetDirectoryName(destination));

                if (fsResult)
                {
                    IStorageFile file = (StorageFile)source;
                    FilesystemResult<StorageFile> fsResultCopy = await FilesystemTasks.Wrap(() => file.CopyAsync(fsResult.Result, source.Name, NameCollisionOption.GenerateUniqueName).AsTask());

                    if (fsResultCopy)
                    {
                        copiedItem = fsResultCopy.Result;
                    }
                    else if (fsResultCopy.ErrorCode == FilesystemErrorCode.ERROR_UNAUTHORIZED)
                    {
                        // Try again with CopyFileFromApp
                        if (NativeFileOperationsHelper.CopyFileFromApp(source.Path, destination, true))
                        {
                            copiedItem = source; // Dangerous - the provided item may be different than output result!
                        }
                        else
                        {
                            Debug.WriteLine(System.Runtime.InteropServices.Marshal.GetLastWin32Error());
                        }
                    }
                    else
                    {
                        errorCode?.Report(fsResultCopy.ErrorCode);
                    }
                }
            }

            if (Path.GetDirectoryName(destination) == _associatedInstance.FilesystemViewModel.WorkingDirectory)
            {
                if (copiedItem != null)
                {
                    List<ListedItem> copiedListedItems = _associatedInstance.FilesystemViewModel.FilesAndFolders.Where(listedItem => copiedItem.Path.Contains(listedItem.ItemPath)).ToList();

                    _associatedInstance.ContentPage.AddSelectedItemsOnUi(copiedListedItems);
                    _associatedInstance.ContentPage.FocusSelectedItems();
                }
            }

            progress?.Report(100.0f);

            return new StorageHistory(FileOperationType.Copy, source.Path, copiedItem.Path);
        }

        public async Task<IStorageHistory> MoveAsync(IStorageItem source, string destination, IProgress<float> progress, IProgress<FilesystemErrorCode> errorCode, CancellationToken cancellationToken)
        {
            IStorageHistory history = await CopyAsync(source, destination, progress, errorCode, cancellationToken);

            FilesystemResult fsResultDelete = (FilesystemResult)false;
            if (string.IsNullOrWhiteSpace(source.Path))
            {
                // Can't move (only copy) files from MTP devices because:
                // StorageItems returned in DataPackageView are read-only
                // The item.Path property will be empty and there's no way of retrieving a new StorageItem with R/W access
                errorCode?.Report(FilesystemErrorCode.ERROR_SUCCESS | FilesystemErrorCode.ERROR_INPROGRESS);
            }
            if (source.IsOfType(StorageItemTypes.File))
            {
                // If we reached this we are not in an MTP device, using StorageFile.* is ok here
                fsResultDelete = await _associatedInstance.FilesystemViewModel.GetFileFromPathAsync(source.Path)
                    .OnSuccess(t => t.DeleteAsync(StorageDeleteOption.PermanentDelete).AsTask());
            }
            else if (source.IsOfType(StorageItemTypes.Folder))
            {
                // If we reached this we are not in an MTP device, using StorageFolder.* is ok here
                fsResultDelete = await _associatedInstance.FilesystemViewModel.GetFolderFromPathAsync(source.Path)
                    .OnSuccess(t => t.DeleteAsync(StorageDeleteOption.PermanentDelete).AsTask());
            }

            if (fsResultDelete == FilesystemErrorCode.ERROR_UNAUTHORIZED)
            {
                // Try again with DeleteFileFromApp
                if (!NativeFileOperationsHelper.DeleteFileFromApp(source.Path))
                {
                    Debug.WriteLine(System.Runtime.InteropServices.Marshal.GetLastWin32Error());
                }
            }
            else if (fsResultDelete == FilesystemErrorCode.ERROR_NOTFOUND)
            {
                // File or Folder was moved/deleted in the meantime
                errorCode?.Report(fsResultDelete);
            }

            progress?.Report(100.0f);

            return new StorageHistory(FileOperationType.Move, history.Source, history.Destination);
        }

        public async Task<IStorageHistory> DeleteAsync(IStorageItem source, IProgress<float> progress, IProgress<FilesystemErrorCode> errorCode, bool permanently, CancellationToken cancellationToken)
        {
            bool deleteFromRecycleBin = _associatedInstance.FilesystemViewModel.WorkingDirectory.StartsWith(App.AppSettings.RecycleBinPath);
            //if (deleteFromRecycleBin) // TODO: Causes issues with Undo.Restore
            //    permanently = true;

            FilesystemResult fsResult = FilesystemErrorCode.ERROR_INPROGRESS;
            FilesystemItemType itemType = source.IsOfType(StorageItemTypes.File) ? FilesystemItemType.File : FilesystemItemType.Directory;

            errorCode?.Report(fsResult);
            progress?.Report(0.0f);

            if (source.IsOfType(StorageItemTypes.File))
            {
                fsResult = await _associatedInstance.FilesystemViewModel.GetFileFromPathAsync(source.Path)
                    .OnSuccess((t) => t.DeleteAsync(permanently ? StorageDeleteOption.PermanentDelete : StorageDeleteOption.Default).AsTask());
            }
            else if (source.IsOfType(StorageItemTypes.Folder))
            {
                fsResult = await _associatedInstance.FilesystemViewModel.GetFolderFromPathAsync(source.Path)
                    .OnSuccess((t) => t.DeleteAsync(permanently ? StorageDeleteOption.PermanentDelete : StorageDeleteOption.Default).AsTask());
            }
            errorCode?.Report(fsResult);

            if (fsResult == FilesystemErrorCode.ERROR_UNAUTHORIZED)
            {
                if (permanently)
                {
                    // Try again with fulltrust process
                    if (_associatedInstance.FilesystemViewModel.Connection != null)
                    {
                        AppServiceResponse response = await _associatedInstance.FilesystemViewModel.Connection.SendMessageAsync(new ValueSet()
                        {
                            { "Arguments", "FileOperation" },
                            { "fileop", "MoveToBin" },
                            { "filepath", source.Path }
                        });
                        fsResult = (FilesystemResult)(response.Status == AppServiceResponseStatus.Success);
                    }
                }
                else
                {
                    // Try again with DeleteFileFromApp
                    if (!NativeFileOperationsHelper.DeleteFileFromApp(source.Path))
                    {
                        Debug.WriteLine(System.Runtime.InteropServices.Marshal.GetLastWin32Error());
                    }
                    else
                    {
                        fsResult = (FilesystemResult)true;
                    }
                }
            }
            else if (fsResult == FilesystemErrorCode.ERROR_INUSE)
            {
                // TODO: retry or show dialog
                await DialogDisplayHelper.ShowDialogAsync("FileInUseDeleteDialog/Title".GetLocalized(), "FileInUseDeleteDialog/Text".GetLocalized());
            }

            if (deleteFromRecycleBin)
            {
                // Recycle bin also stores a file starting with $I for each item
                string iFilePath = Path.Combine(Path.GetDirectoryName(source.Path), Path.GetFileName(source.Path).Replace("$R", "$I"));
                await _associatedInstance.FilesystemViewModel.GetFileFromPathAsync(iFilePath)
                    .OnSuccess(t => t.DeleteAsync(StorageDeleteOption.PermanentDelete).AsTask());
            }
            errorCode?.Report(fsResult);
            progress?.Report(100.0f);

            if (fsResult)
            {
                await _associatedInstance.FilesystemViewModel.RemoveFileOrFolderAsync(source.Path);

                if (!permanently)
                {
                    // Enumerate Recycle Bin
                    List<ShellFileItem> items = await RecycleBinHelpers.EnumerateRecycleBin(this._connection);

                    // Get name matching files
                    List<ShellFileItem> nameMatchItems = items.Where((item) => item.FileName == source.Name).ToList();

                    // Get newest file
                    ShellFileItem item = nameMatchItems.Where((item) => item.RecycleDate != null).OrderBy((item) => item.RecycleDate).FirstOrDefault();

                    return new StorageHistory(FileOperationType.Recycle, source.Path, $"{item.RecyclePath}|{itemType.ToString()}");
                }

                return new StorageHistory(FileOperationType.Delete, source.Path, null);
            }
            else
            {
                // Stop at first error
                return null;
            }
        }

        public async Task<IStorageHistory> RenameAsync(IStorageItem source, string newName, NameCollisionOption collision, IProgress<FilesystemErrorCode> errorCode, CancellationToken cancellationToken)
        {
            try
            {
                string originalSource = source.Path;
                await source.RenameAsync(newName, collision);

                errorCode?.Report(FilesystemErrorCode.ERROR_SUCCESS);
                return new StorageHistory(FileOperationType.Rename, originalSource, source.Path);
            }
            catch (Exception e)
            {
                errorCode?.Report(FilesystemTasks.GetErrorCode(e));
                return null;
            }
        }

        public async Task<IStorageHistory> RestoreFromTrashAsync(string source, string destination, IProgress<float> progress, IProgress<FilesystemErrorCode> errorCode, CancellationToken cancellationToken)
        {
            FilesystemResult fsResult = FilesystemErrorCode.ERROR_INPROGRESS;
            errorCode?.Report(fsResult);
            FilesystemItemType itemType = EnumExtensions.GetEnum<FilesystemItemType>(source.Split('|')[1]);
            source = source.Split('|')[0];

            if (itemType == FilesystemItemType.Directory)
            {
                FilesystemResult<StorageFolder> sourceFolder = await _associatedInstance.FilesystemViewModel.GetFolderFromPathAsync(source);
                fsResult = sourceFolder.ErrorCode;
                errorCode?.Report(fsResult);

                if (sourceFolder)
                {
                    fsResult = await FilesystemTasks.Wrap(() => MoveAsync(sourceFolder.Result, destination, progress, errorCode, cancellationToken))
                                .OnSuccess(t => sourceFolder.Result.DeleteAsync(StorageDeleteOption.PermanentDelete).AsTask());
                }
                errorCode?.Report(fsResult);
            }
            else
            {
                FilesystemResult<StorageFile> sourceFile = await _associatedInstance.FilesystemViewModel.GetFileFromPathAsync(source);
                fsResult = sourceFile.ErrorCode;
                errorCode?.Report(fsResult);

                if (sourceFile)
                {
                    fsResult = await FilesystemTasks.Wrap(async () => sourceFile.Result.MoveAsync((IStorageFolder)(await Path.GetDirectoryName(destination).ToStorageItem()), Path.GetFileName(destination), NameCollisionOption.GenerateUniqueName).AsTask());
                }
                errorCode?.Report(fsResult);
            }

            // Recycle bin also stores a file starting with $I for each item
            string iFilePath = Path.Combine(Path.GetDirectoryName(source), Path.GetFileName(source).Replace("$R", "$I"));
            await _associatedInstance.FilesystemViewModel.GetFileFromPathAsync(iFilePath)
                .OnSuccess(iFile => iFile.DeleteAsync().AsTask());

            errorCode?.Report(fsResult);
            if (fsResult != FilesystemErrorCode.ERROR_SUCCESS)
            {
                if (((FilesystemErrorCode)fsResult).HasFlag(FilesystemErrorCode.ERROR_UNAUTHORIZED))
                {
                    await DialogDisplayHelper.ShowDialogAsync("AccessDeniedDeleteDialog/Title".GetLocalized(), "AccessDeniedDeleteDialog/Text".GetLocalized());
                }
                else if (((FilesystemErrorCode)fsResult).HasFlag(FilesystemErrorCode.ERROR_UNAUTHORIZED))
                {
                    await DialogDisplayHelper.ShowDialogAsync("FileNotFoundDialog/Title".GetLocalized(), "FileNotFoundDialog/Text".GetLocalized());
                }
                else if (((FilesystemErrorCode)fsResult).HasFlag(FilesystemErrorCode.ERROR_ALREADYEXIST))
                {
                    await DialogDisplayHelper.ShowDialogAsync("ItemAlreadyExistsDialogTitle".GetLocalized(), "ItemAlreadyExistsDialogContent".GetLocalized());
                }
            }

            //if (Path.GetFileName(source.ItemOriginalPath) != Path.GetFileName(destination)) // Different names
            //    return new StorageHistory(FileOperationType.Restore, source.ItemPath, Path.Combine(destination, Path.GetFileName(source.ItemOriginalPath)));
            //else
            return new StorageHistory(FileOperationType.Restore, source, destination);
        }

        #endregion

        public async static Task<StorageFolder> CloneDirectoryAsync(IStorageFolder sourceFolder, IStorageFolder destinationFolder, string sourceRootName)
        {
            StorageFolder createdRoot = await destinationFolder.CreateFolderAsync(sourceRootName, CreationCollisionOption.GenerateUniqueName);
            destinationFolder = createdRoot;

            foreach (IStorageFile fileInSourceDir in await sourceFolder.GetFilesAsync())
            {
                await fileInSourceDir.CopyAsync(destinationFolder, fileInSourceDir.Name, NameCollisionOption.GenerateUniqueName);
            }

            foreach (IStorageFolder folderinSourceDir in await sourceFolder.GetFoldersAsync())
            {
                await CloneDirectoryAsync(folderinSourceDir, destinationFolder, folderinSourceDir.Name);
            }

            return createdRoot;
        }

        public static async Task<long> GetItemSize(IStorageItem item)
        {
            if (item.IsOfType(StorageItemTypes.Folder))
            {
                return await CalculateFolderSizeAsync(item.Path);
            }
            else
            {
                return CalculateFileSize(item.Path);
            }
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

            int count = 0;
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

        #region IDisposable

        public void Dispose()
        {
            this._connection?.Dispose();
            this._associatedInstance?.Dispose();

            this._associatedInstance = null;
        }

        #endregion
    }
}
