using Files.Common;
using Files.Enums;
using Files.Extensions;
using Files.Filesystem.FilesystemHistory;
using Files.Helpers;
using Microsoft.Toolkit.Uwp.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml.Controls;
using FileAttributes = System.IO.FileAttributes;

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

        private IShellPage associatedInstance;

        private RecycleBinHelpers recycleBinHelpers;

        #endregion Private Members

        #region Constructor

        public FilesystemOperations(IShellPage associatedInstance)
        {
            this.associatedInstance = associatedInstance;
            recycleBinHelpers = new RecycleBinHelpers(this.associatedInstance);
        }

        #endregion Constructor

        #region IFilesystemOperations

        public async Task<IStorageHistory> CreateAsync(IStorageItemWithPath source, IProgress<FilesystemErrorCode> errorCode, CancellationToken cancellationToken)
        {
            try
            {
                switch (source.ItemType)
                {
                    case FilesystemItemType.File:
                        {
                            var newEntryInfo = await RegistryHelper.GetNewContextMenuEntryForType(Path.GetExtension(source.Path));
                            if (newEntryInfo == null)
                            {
                                StorageFolder folder = await associatedInstance.FilesystemViewModel.GetFolderFromPathAsync(Path.GetDirectoryName(source.Path));
                                await folder.CreateFileAsync(Path.GetFileName(source.Path));
                            }
                            else
                            {
                                await newEntryInfo.Create(source.Path, associatedInstance);
                            }

                            break;
                        }

                    case FilesystemItemType.Directory:
                        {
                            StorageFolder folder = await associatedInstance.FilesystemViewModel.GetFolderFromPathAsync(Path.GetDirectoryName(source.Path));
                            await folder.CreateFolderAsync(Path.GetFileName(source.Path));

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
                return new StorageHistory(FileOperationType.CreateNew, source.CreateEnumerable(), null);
            }
            catch (Exception e)
            {
                errorCode?.Report(FilesystemTasks.GetErrorCode(e));
                return null;
            }
        }

        public async Task<IStorageHistory> CopyAsync(IStorageItem source,
                                                     string destination,
                                                     IProgress<float> progress,
                                                     IProgress<FilesystemErrorCode> errorCode,
                                                     CancellationToken cancellationToken)
        {
            return await CopyAsync(source.FromStorageItem(),
                                                    destination,
                                                    progress,
                                                    errorCode,
                                                    cancellationToken);
        }

        public async Task<IStorageHistory> CopyAsync(IStorageItemWithPath source,
                                                     string destination,
                                                     IProgress<float> progress,
                                                     IProgress<FilesystemErrorCode> errorCode,
                                                     CancellationToken cancellationToken)
        {
            if (associatedInstance.FilesystemViewModel.WorkingDirectory.StartsWith(App.AppSettings.RecycleBinPath))
            {
                errorCode?.Report(FilesystemErrorCode.ERROR_UNAUTHORIZED);
                progress?.Report(100.0f);

                // Do not paste files and folders inside the recycle bin
                await DialogDisplayHelper.ShowDialogAsync(
                    "ErrorDialogThisActionCannotBeDone".GetLocalized(),
                    "ErrorDialogUnsupportedOperation".GetLocalized());
                return null;
            }

            IStorageItem copiedItem = null;
            //long itemSize = await FilesystemHelpers.GetItemSize(await source.ToStorageItem(associatedInstance));

            if (source.ItemType == FilesystemItemType.Directory)
            {
                if (!string.IsNullOrWhiteSpace(source.Path) &&
                    Path.GetDirectoryName(destination).IsSubPathOf(source.Path)) // We check if user tried to copy anything above the source.ItemPath
                {
                    var destinationName = destination.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries).Last();
                    var sourceName = source.Path.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries).Last();
                    ContentDialog dialog = new ContentDialog()
                    {
                        Title = "ErrorDialogThisActionCannotBeDone".GetLocalized(),
                        Content = "ErrorDialogTheDestinationFolder".GetLocalized() + " (" + destinationName + ") " + "ErrorDialogIsASubfolder".GetLocalized() + " (" + sourceName + ")",
                        //PrimaryButtonText = "ErrorDialogSkip".GetLocalized(),
                        CloseButtonText = "ErrorDialogCancel".GetLocalized()
                    };

                    ContentDialogResult result = await dialog.ShowAsync();

                    if (result == ContentDialogResult.Primary)
                    {
                        progress?.Report(100.0f);
                        errorCode?.Report(FilesystemErrorCode.ERROR_INPROGRESS | FilesystemErrorCode.ERROR_SUCCESS);
                    }
                    else
                    {
                        progress?.Report(100.0f);
                        errorCode?.Report(FilesystemErrorCode.ERROR_INPROGRESS | FilesystemErrorCode.ERROR_GENERIC);
                    }
                    return null;
                }
                else
                {
                    var fsSourceFolder = await source.ToStorageItemResult(associatedInstance);
                    var fsDestinationFolder = await associatedInstance.FilesystemViewModel.GetFolderFromPathAsync(Path.GetDirectoryName(destination));
                    var fsResult = (FilesystemResult)(fsSourceFolder.ErrorCode | fsDestinationFolder.ErrorCode);

                    if (fsResult)
                    {
                        var fsCopyResult = await FilesystemTasks.Wrap(() => CloneDirectoryAsync((StorageFolder)fsSourceFolder, (StorageFolder)fsDestinationFolder, fsSourceFolder.Result.Name, CreationCollisionOption.FailIfExists));
                        if (fsCopyResult == FilesystemErrorCode.ERROR_ALREADYEXIST)
                        {
                            var ItemAlreadyExistsDialog = new ContentDialog()
                            {
                                Title = "ItemAlreadyExistsDialogTitle".GetLocalized(),
                                Content = "ItemAlreadyExistsDialogContent".GetLocalized(),
                                PrimaryButtonText = "ItemAlreadyExistsDialogPrimaryButtonText".GetLocalized(),
                                SecondaryButtonText = "ItemAlreadyExistsDialogSecondaryButtonText".GetLocalized(),
                                CloseButtonText = "ItemAlreadyExistsDialogCloseButtonText".GetLocalized()
                            };

                            ContentDialogResult result = await ItemAlreadyExistsDialog.ShowAsync();

                            if (result == ContentDialogResult.Primary)
                            {
                                fsCopyResult = await FilesystemTasks.Wrap(() => CloneDirectoryAsync((StorageFolder)fsSourceFolder, (StorageFolder)fsDestinationFolder, fsSourceFolder.Result.Name, CreationCollisionOption.GenerateUniqueName));
                            }
                            else if (result == ContentDialogResult.Secondary)
                            {
                                fsCopyResult = await FilesystemTasks.Wrap(() => CloneDirectoryAsync((StorageFolder)fsSourceFolder, (StorageFolder)fsDestinationFolder, fsSourceFolder.Result.Name, CreationCollisionOption.ReplaceExisting));
                                return null; // Cannot undo overwrite operation
                            }
                            else
                            {
                                return null;
                            }
                        }
                        if (fsCopyResult)
                        {
                            if (FolderHelpers.CheckFolderForHiddenAttribute(source.Path))
                            {
                                // The source folder was hidden, apply hidden attribute to destination
                                NativeFileOperationsHelper.SetFileAttribute(fsCopyResult.Result.Path, FileAttributes.Hidden);
                            }
                            copiedItem = (StorageFolder)fsCopyResult;
                        }
                        fsResult = fsCopyResult;
                    }
                    errorCode?.Report(fsResult.ErrorCode);
                    if (!fsResult)
                    {
                        return null;
                    }
                }
            }
            else if (source.ItemType == FilesystemItemType.File)
            {
                FilesystemResult<StorageFolder> destinationResult = await associatedInstance.FilesystemViewModel.GetFolderFromPathAsync(Path.GetDirectoryName(destination));
                var sourceResult = await source.ToStorageItemResult(associatedInstance);
                var fsResult = (FilesystemResult)(sourceResult.ErrorCode | destinationResult.ErrorCode);

                if (fsResult)
                {
                    var file = (StorageFile)sourceResult;
                    var fsResultCopy = await FilesystemTasks.Wrap(() => file.CopyAsync(destinationResult.Result, Path.GetFileName(file.Name), NameCollisionOption.FailIfExists).AsTask());
                    if (fsResultCopy == FilesystemErrorCode.ERROR_ALREADYEXIST)
                    {
                        var ItemAlreadyExistsDialog = new ContentDialog()
                        {
                            Title = "ItemAlreadyExistsDialogTitle".GetLocalized(),
                            Content = "ItemAlreadyExistsDialogContent".GetLocalized(),
                            PrimaryButtonText = "ItemAlreadyExistsDialogPrimaryButtonText".GetLocalized(),
                            SecondaryButtonText = "ItemAlreadyExistsDialogSecondaryButtonText".GetLocalized(),
                            CloseButtonText = "ItemAlreadyExistsDialogCloseButtonText".GetLocalized()
                        };

                        ContentDialogResult result = await ItemAlreadyExistsDialog.ShowAsync();

                        if (result == ContentDialogResult.Primary)
                        {
                            fsResultCopy = await FilesystemTasks.Wrap(() => file.CopyAsync(destinationResult.Result, Path.GetFileName(file.Name), NameCollisionOption.GenerateUniqueName).AsTask());
                        }
                        else if (result == ContentDialogResult.Secondary)
                        {
                            fsResultCopy = await FilesystemTasks.Wrap(() => file.CopyAsync(destinationResult.Result, Path.GetFileName(file.Name), NameCollisionOption.ReplaceExisting).AsTask());
                            return null; // Cannot undo overwrite operation
                        }
                        else
                        {
                            return null;
                        }
                    }
                    if (fsResultCopy)
                    {
                        copiedItem = fsResultCopy.Result;
                    }
                    fsResult = fsResultCopy;
                }
                if (fsResult == FilesystemErrorCode.ERROR_UNAUTHORIZED || fsResult == FilesystemErrorCode.ERROR_GENERIC)
                {
                    // Try again with CopyFileFromApp
                    if (NativeFileOperationsHelper.CopyFileFromApp(source.Path, destination, true))
                    {
                        fsResult = (FilesystemResult)true;
                    }
                    else
                    {
                        Debug.WriteLine(System.Runtime.InteropServices.Marshal.GetLastWin32Error());
                    }
                }
                errorCode?.Report(fsResult.ErrorCode);
                if (!fsResult)
                {
                    return null;
                }
            }

            if (Path.GetDirectoryName(destination) == associatedInstance.FilesystemViewModel.WorkingDirectory)
            {
                if (copiedItem != null)
                {
                    List<ListedItem> copiedListedItems = associatedInstance.FilesystemViewModel.FilesAndFolders
                        .Where(listedItem => copiedItem.Path.Contains(listedItem.ItemPath)).ToList();

                    if (copiedListedItems.Count > 0)
                    {
                        associatedInstance.ContentPage.AddSelectedItemsOnUi(copiedListedItems);
                        associatedInstance.ContentPage.FocusSelectedItems();
                    }
                }
            }

            progress?.Report(100.0f);

            var pathWithType = copiedItem.FromStorageItem(destination, source.ItemType);

            return new StorageHistory(FileOperationType.Copy, source, pathWithType);
        }

        public async Task<IStorageHistory> MoveAsync(IStorageItem source,
                                                     string destination,
                                                     IProgress<float> progress,
                                                     IProgress<FilesystemErrorCode> errorCode,
                                                     CancellationToken cancellationToken)
        {
            return await MoveAsync(source.FromStorageItem(),
                                                    destination,
                                                    progress,
                                                    errorCode,
                                                    cancellationToken);
        }

        public async Task<IStorageHistory> MoveAsync(IStorageItemWithPath source,
                                                     string destination,
                                                     IProgress<float> progress,
                                                     IProgress<FilesystemErrorCode> errorCode,
                                                     CancellationToken cancellationToken)
        {
            if (source.Path == destination)
            {
                progress?.Report(100.0f);
                errorCode?.Report(FilesystemErrorCode.ERROR_SUCCESS);
                return null;
            }

            IStorageHistory history = await CopyAsync(source, destination, progress, errorCode, cancellationToken);
            if (history == null)
            {
                // If copy was not performed we don't continue to delete to prevent data loss
                return null;
            }

            if (string.IsNullOrWhiteSpace(source.Path))
            {
                // Can't move (only copy) files from MTP devices because:
                // StorageItems returned in DataPackageView are read-only
                // The item.Path property will be empty and there's no way of retrieving a new StorageItem with R/W access
                errorCode?.Report(FilesystemErrorCode.ERROR_SUCCESS | FilesystemErrorCode.ERROR_INPROGRESS);
            }

            await DeleteAsync(source, progress, errorCode, true, cancellationToken);

            progress?.Report(100.0f);

            return new StorageHistory(FileOperationType.Move, history.Source, history.Destination);
        }

        public async Task<IStorageHistory> DeleteAsync(IStorageItem source,
                                                       IProgress<float> progress,
                                                       IProgress<FilesystemErrorCode> errorCode,
                                                       bool permanently,
                                                       CancellationToken cancellationToken)
        {
            return await DeleteAsync(source.FromStorageItem(),
                                                      progress,
                                                      errorCode,
                                                      permanently,
                                                      cancellationToken);
        }

        public async Task<IStorageHistory> DeleteAsync(IStorageItemWithPath source,
                                                       IProgress<float> progress,
                                                       IProgress<FilesystemErrorCode> errorCode,
                                                       bool permanently,
                                                       CancellationToken cancellationToken)
        {
            bool deleteFromRecycleBin = recycleBinHelpers.IsPathUnderRecycleBin(source.Path);

            FilesystemResult fsResult = FilesystemErrorCode.ERROR_INPROGRESS;

            errorCode?.Report(fsResult);
            progress?.Report(0.0f);

            if (source.ItemType == FilesystemItemType.File)
            {
                fsResult = await associatedInstance.FilesystemViewModel.GetFileFromPathAsync(source.Path)
                    .OnSuccess((t) => t.DeleteAsync(permanently ? StorageDeleteOption.PermanentDelete : StorageDeleteOption.Default).AsTask());
            }
            else if (source.ItemType == FilesystemItemType.Directory)
            {
                fsResult = await associatedInstance.FilesystemViewModel.GetFolderFromPathAsync(source.Path)
                    .OnSuccess((t) => t.DeleteAsync(permanently ? StorageDeleteOption.PermanentDelete : StorageDeleteOption.Default).AsTask());
            }

            errorCode?.Report(fsResult);

            if (fsResult == FilesystemErrorCode.ERROR_UNAUTHORIZED)
            {
                // Try again with fulltrust process
                if (associatedInstance.FilesystemViewModel.Connection != null)
                {
                    AppServiceResponse response = await associatedInstance.FilesystemViewModel.Connection.SendMessageAsync(new ValueSet()
                        {
                            { "Arguments", "FileOperation" },
                            { "fileop", "DeleteItem" },
                            { "filepath", source.Path },
                            { "permanently", permanently }
                        });
                    fsResult = (FilesystemResult)(response.Status == AppServiceResponseStatus.Success
                        && response.Message.Get("Success", false));
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
                await associatedInstance.FilesystemViewModel.GetFileFromPathAsync(iFilePath)
                    .OnSuccess(t => t.DeleteAsync(StorageDeleteOption.PermanentDelete).AsTask());
            }
            errorCode?.Report(fsResult);
            progress?.Report(100.0f);

            if (fsResult)
            {
                await associatedInstance.FilesystemViewModel.RemoveFileOrFolderAsync(source.Path);

                if (!permanently)
                {
                    // Enumerate Recycle Bin
                    List<ShellFileItem> items = await recycleBinHelpers.EnumerateRecycleBin();
                    List<ShellFileItem> nameMatchItems = new List<ShellFileItem>();

                    // Get name matching files
                    if (items != null)
                    {
                        if (Path.GetExtension(source.Path) == ".lnk" || Path.GetExtension(source.Path) == ".url") // We need to check if it is a shortcut file
                        {
                            nameMatchItems = items.Where((item) => item.FilePath == Path.Combine(Path.GetDirectoryName(source.Path), Path.GetFileNameWithoutExtension(source.Path))).ToList();
                        }
                        else
                        {
                            nameMatchItems = items.Where((item) => item.FilePath == source.Path).ToList();
                        }
                    }

                    // Get newest file
                    ShellFileItem item = nameMatchItems.Where((item) => item.RecycleDate != null).OrderBy((item) => item.RecycleDate).FirstOrDefault();

                    return new StorageHistory(FileOperationType.Recycle, source, StorageItemHelpers.FromPathAndType(item?.RecyclePath, source.ItemType));
                }

                return new StorageHistory(FileOperationType.Delete, source, null);
            }
            else
            {
                // Stop at first error
                return null;
            }
        }

        public async Task<IStorageHistory> RenameAsync(IStorageItem source,
                                                       string newName,
                                                       NameCollisionOption collision,
                                                       IProgress<FilesystemErrorCode> errorCode,
                                                       CancellationToken cancellationToken)
        {
            return await RenameAsync(StorageItemHelpers.FromStorageItem(source), newName, collision, errorCode, cancellationToken);
        }

        public async Task<IStorageHistory> RenameAsync(IStorageItemWithPath source,
                                                       string newName,
                                                       NameCollisionOption collision,
                                                       IProgress<FilesystemErrorCode> errorCode,
                                                       CancellationToken cancellationToken)
        {
            if (Path.GetFileName(source.Path) == newName && collision == NameCollisionOption.FailIfExists)
            {
                errorCode?.Report(FilesystemErrorCode.ERROR_ALREADYEXIST);
                return null;
            }

            if (!string.IsNullOrWhiteSpace(newName)
                && !FilesystemHelpers.ContainsRestrictedCharacters(newName)
                && !FilesystemHelpers.ContainsRestrictedFileName(newName))
            {
                var renamed = await source.ToStorageItemResult(associatedInstance)
                    .OnSuccess(async (t) =>
                    {
                        await t.RenameAsync(newName, collision);
                        return t;
                    });

                if (renamed)
                {
                    errorCode?.Report(FilesystemErrorCode.ERROR_SUCCESS);
                    return new StorageHistory(FileOperationType.Rename, source, renamed.Result.FromStorageItem());
                }
                else if (renamed == FilesystemErrorCode.ERROR_UNAUTHORIZED)
                {
                    // Try again with MoveFileFromApp
                    var destination = Path.Combine(Path.GetDirectoryName(source.Path), newName);
                    if (NativeFileOperationsHelper.MoveFileFromApp(source.Path, destination))
                    {
                        errorCode?.Report(FilesystemErrorCode.ERROR_SUCCESS);
                        return new StorageHistory(FileOperationType.Rename, source, StorageItemHelpers.FromPathAndType(destination, source.ItemType));
                    }
                    else
                    {
                        Debug.WriteLine(System.Runtime.InteropServices.Marshal.GetLastWin32Error());
                    }
                }
                else if (renamed == FilesystemErrorCode.ERROR_NOTAFILE || renamed == FilesystemErrorCode.ERROR_NOTAFOLDER)
                {
                    await DialogDisplayHelper.ShowDialogAsync("RenameError/NameInvalid/Title".GetLocalized(), "RenameError/NameInvalid/Text".GetLocalized());
                }
                else if (renamed == FilesystemErrorCode.ERROR_NAMETOOLONG)
                {
                    await DialogDisplayHelper.ShowDialogAsync("RenameError/TooLong/Title".GetLocalized(), "RenameError/TooLong/Text".GetLocalized());
                }
                else if (renamed == FilesystemErrorCode.ERROR_INUSE)
                {
                    // TODO: proper dialog, retry
                    await DialogDisplayHelper.ShowDialogAsync("FileInUseDeleteDialog/Title".GetLocalized(), "");
                }
                else if (renamed == FilesystemErrorCode.ERROR_NOTFOUND)
                {
                    await DialogDisplayHelper.ShowDialogAsync("RenameError/ItemDeleted/Title".GetLocalized(), "RenameError/ItemDeleted/Text".GetLocalized());
                }
                else if (renamed == FilesystemErrorCode.ERROR_ALREADYEXIST)
                {
                    var ItemAlreadyExistsDialog = new ContentDialog()
                    {
                        Title = "ItemAlreadyExistsDialogTitle".GetLocalized(),
                        Content = "ItemAlreadyExistsDialogContent".GetLocalized(),
                        PrimaryButtonText = "ItemAlreadyExistsDialogPrimaryButtonText".GetLocalized(),
                        SecondaryButtonText = "ItemAlreadyExistsDialogSecondaryButtonText".GetLocalized(),
                        CloseButtonText = "ItemAlreadyExistsDialogCloseButtonText".GetLocalized()
                    };

                    ContentDialogResult result = await ItemAlreadyExistsDialog.ShowAsync();

                    if (result == ContentDialogResult.Primary)
                    {
                        return await RenameAsync(source, newName, NameCollisionOption.GenerateUniqueName, errorCode, cancellationToken);
                    }
                    else if (result == ContentDialogResult.Secondary)
                    {
                        return await RenameAsync(source, newName, NameCollisionOption.ReplaceExisting, errorCode, cancellationToken);
                    }
                }
                errorCode?.Report(renamed);
            }

            return null;
        }

        public async Task<IStorageHistory> RestoreFromTrashAsync(IStorageItemWithPath source,
                                                                 string destination,
                                                                 IProgress<float> progress,
                                                                 IProgress<FilesystemErrorCode> errorCode,
                                                                 CancellationToken cancellationToken)
        {
            FilesystemResult fsResult = FilesystemErrorCode.ERROR_INPROGRESS;
            errorCode?.Report(fsResult);

            if (source.ItemType == FilesystemItemType.Directory)
            {
                FilesystemResult<StorageFolder> sourceFolder = await associatedInstance.FilesystemViewModel.GetFolderFromPathAsync(source.Path);
                FilesystemResult<StorageFolder> destinationFolder = await associatedInstance.FilesystemViewModel.GetFolderFromPathAsync(Path.GetDirectoryName(destination));

                fsResult = sourceFolder.ErrorCode | destinationFolder.ErrorCode;
                errorCode?.Report(fsResult);

                if (fsResult)
                {
                    fsResult = await FilesystemTasks.Wrap(() =>
                    {
                        return MoveDirectoryAsync(sourceFolder.Result,
                                                  destinationFolder.Result,
                                                  Path.GetFileName(destination),
                                                  CreationCollisionOption.FailIfExists);
                    }).OnSuccess(t => sourceFolder.Result.DeleteAsync(StorageDeleteOption.PermanentDelete).AsTask()); // TODO: we could use here FilesystemHelpers with registerHistory false?
                }
                errorCode?.Report(fsResult);
            }
            else
            {
                FilesystemResult<StorageFile> sourceFile = await associatedInstance.FilesystemViewModel.GetFileFromPathAsync(source.Path);
                FilesystemResult<StorageFolder> destinationFolder = await associatedInstance.FilesystemViewModel.GetFolderFromPathAsync(Path.GetDirectoryName(destination));

                fsResult = sourceFile.ErrorCode | destinationFolder.ErrorCode;
                errorCode?.Report(fsResult);

                if (fsResult)
                {
                    fsResult = await FilesystemTasks.Wrap(() =>
                    {
                        return sourceFile.Result.MoveAsync(destinationFolder.Result,
                                                           Path.GetFileName(destination),
                                                           NameCollisionOption.GenerateUniqueName).AsTask();
                    });
                }
                else if (fsResult == FilesystemErrorCode.ERROR_UNAUTHORIZED)
                {
                    // Try again with MoveFileFromApp
                    fsResult = (FilesystemResult)NativeFileOperationsHelper.MoveFileFromApp(source.Path, destination);
                }
                errorCode?.Report(fsResult);
            }

            if (fsResult)
            {
                // Recycle bin also stores a file starting with $I for each item
                string iFilePath = Path.Combine(Path.GetDirectoryName(source.Path), Path.GetFileName(source.Path).Replace("$R", "$I"));
                await associatedInstance.FilesystemViewModel.GetFileFromPathAsync(iFilePath)
                    .OnSuccess(iFile => iFile.DeleteAsync().AsTask());
            }

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

            return new StorageHistory(FileOperationType.Restore, source, StorageItemHelpers.FromPathAndType(destination, source.ItemType));
        }

        #endregion IFilesystemOperations

        #region Helpers

        private async static Task<StorageFolder> CloneDirectoryAsync(IStorageFolder sourceFolder, IStorageFolder destinationFolder, string sourceRootName, CreationCollisionOption collision = CreationCollisionOption.FailIfExists)
        {
            StorageFolder createdRoot = await destinationFolder.CreateFolderAsync(sourceRootName, collision);
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

        private static async Task<StorageFolder> MoveDirectoryAsync(IStorageFolder sourceFolder, IStorageFolder destinationDirectory, string sourceRootName, CreationCollisionOption collision = CreationCollisionOption.FailIfExists)
        {
            StorageFolder createdRoot = await destinationDirectory.CreateFolderAsync(sourceRootName, collision);
            destinationDirectory = createdRoot;

            foreach (StorageFile fileInSourceDir in await sourceFolder.GetFilesAsync())
            {
                await fileInSourceDir.MoveAsync(destinationDirectory, fileInSourceDir.Name, NameCollisionOption.GenerateUniqueName);
            }

            foreach (StorageFolder folderinSourceDir in await sourceFolder.GetFoldersAsync())
            {
                await MoveDirectoryAsync(folderinSourceDir, destinationDirectory, folderinSourceDir.Name);
            }

            App.JumpList.RemoveFolder(sourceFolder.Path);

            return createdRoot;
        }

        #endregion Helpers

        #region IDisposable

        public void Dispose()
        {
            associatedInstance?.Dispose();
            recycleBinHelpers?.Dispose();

            recycleBinHelpers = null;
            associatedInstance = null;
        }

        #endregion IDisposable
    }
}