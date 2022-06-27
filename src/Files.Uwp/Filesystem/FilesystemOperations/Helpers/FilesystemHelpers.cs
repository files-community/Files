using CommunityToolkit.Mvvm.DependencyInjection;
using Files.Backend.Services;
using Files.Backend.Services.Settings;
using Files.Backend.ViewModels.Dialogs.FileSystemDialog;
using Files.Shared;
using Files.Shared.Enums;
using Files.Shared.Extensions;
using Files.Uwp.Filesystem.FilesystemHistory;
using Files.Uwp.Helpers;
using Files.Uwp.Interacts;
using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.Storage;

namespace Files.Uwp.Filesystem
{
    public class FilesystemHelpers : IFilesystemHelpers
    {
        #region Private Members

        private IShellPage associatedInstance;

        private IFilesystemOperations filesystemOperations;

        private ItemManipulationModel itemManipulationModel => associatedInstance.SlimContentPage?.ItemManipulationModel;

        private RecycleBinHelpers recycleBinHelpers;

        private readonly CancellationToken cancellationToken;

        private Task<NamedPipeAsAppServiceConnection> ServiceConnection => AppServiceConnectionHelper.Instance;

        #region Helpers Members

        private static char[] RestrictedCharacters
        {
            get
            {
                var userSettingsService = Ioc.Default.GetService<IUserSettingsService>();
                if (userSettingsService.PreferencesSettingsService.AreAlternateStreamsVisible)
                {
                    // Allow ":" char
                    return new[] { '\\', '/', '*', '?', '"', '<', '>', '|' };
                }
                return new[] { '\\', '/', ':', '*', '?', '"', '<', '>', '|' };
            }
        }

        private static readonly string[] RestrictedFileNames = new string[]
        {
                "CON", "PRN", "AUX",
                "NUL", "COM1", "COM2",
                "COM3", "COM4", "COM5",
                "COM6", "COM7", "COM8",
                "COM9", "LPT1", "LPT2",
                "LPT3", "LPT4", "LPT5",
                "LPT6", "LPT7", "LPT8", "LPT9"
        };

        #endregion Helpers Members

        #endregion Private Members

        #region Properties

        private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetService<IUserSettingsService>();

        #endregion

        #region Constructor

        public FilesystemHelpers(IShellPage associatedInstance, CancellationToken cancellationToken)
        {
            this.associatedInstance = associatedInstance;
            this.cancellationToken = cancellationToken;
            this.filesystemOperations = new ShellFilesystemOperations(this.associatedInstance);
            this.recycleBinHelpers = new RecycleBinHelpers();
        }

        #endregion Constructor

        #region IFilesystemHelpers

        #region Create

        public async Task<(ReturnResult, IStorageItem)> CreateAsync(IStorageItemWithPath source, bool registerHistory)
        {
            var returnStatus = ReturnResult.InProgress;
            var errorCode = new Progress<FileSystemStatusCode>();
            errorCode.ProgressChanged += (s, e) => returnStatus = returnStatus < ReturnResult.Failed ? e.ToStatus() : returnStatus;

            var result = await filesystemOperations.CreateAsync(source, errorCode, cancellationToken);

            if (registerHistory && !string.IsNullOrWhiteSpace(source.Path))
            {
                App.HistoryWrapper.AddHistory(result.Item1);
            }

            await Task.Yield();
            return (returnStatus, result.Item2);
        }

        #endregion Create

        #region Delete

        public async Task<ReturnResult> DeleteItemsAsync(IEnumerable<IStorageItemWithPath> source, bool showDialog, bool permanently, bool registerHistory)
        {
            source = await source.ToListAsync();

            var returnStatus = ReturnResult.InProgress;

            var deleteFromRecycleBin = source.Select(item => item.Path).Any(path => recycleBinHelpers.IsPathUnderRecycleBin(path));
            var canBeSentToBin = !deleteFromRecycleBin && await recycleBinHelpers.HasRecycleBin(source.FirstOrDefault()?.Path);

            if (((!permanently && !canBeSentToBin) || UserSettingsService.PreferencesSettingsService.ShowConfirmDeleteDialog) && showDialog) // Check if the setting to show a confirmation dialog is on
            {
                var incomingItems = new List<BaseFileSystemDialogItemViewModel>();
                List<ShellFileItem> binItems = null;
                foreach (var src in source)
                {
                    if (recycleBinHelpers.IsPathUnderRecycleBin(src.Path))
                    {
                        binItems ??= await recycleBinHelpers.EnumerateRecycleBin();
                        if (!binItems.IsEmpty()) // Might still be null because we're deserializing the list from Json
                        {
                            var matchingItem = binItems.FirstOrDefault(x => x.RecyclePath == src.Path); // Get original file name
                            incomingItems.Add(new FileSystemDialogDefaultItemViewModel() { SourcePath = src.Path, DisplayName = matchingItem?.FileName ?? src.Name });
                        }
                    }
                    else
                    {
                        incomingItems.Add(new FileSystemDialogDefaultItemViewModel() { SourcePath = src.Path });
                    }
                }

                var dialogViewModel = FileSystemDialogViewModel.GetDialogViewModel(
                    new() { IsInDeleteMode = true },
                    (canBeSentToBin ? permanently : true, canBeSentToBin),
                    FilesystemOperationType.Delete,
                    incomingItems,
                    new());

                var dialogService = Ioc.Default.GetRequiredService<IDialogService>();

                if (await dialogService.ShowDialogAsync(dialogViewModel) != DialogResult.Primary)
                {
                    return ReturnResult.Cancelled; // Return if the result isn't delete
                }

                // Delete selected items if the result is Yes
                permanently = dialogViewModel.DeletePermanently;
            }

            // post the status banner
            var banner = PostBannerHelpers.PostBanner_Delete(source, returnStatus, permanently, false, 0);
            banner.ErrorCode.ProgressChanged += (s, e) => returnStatus = returnStatus < ReturnResult.Failed ? e.ToStatus() : returnStatus;

            var token = banner.CancellationToken;

            var sw = new Stopwatch();
            sw.Start();

            IStorageHistory history = await filesystemOperations.DeleteItemsAsync((IList<IStorageItemWithPath>)source, banner.Progress, banner.ErrorCode, permanently, token);
            ((IProgress<float>)banner.Progress).Report(100.0f);
            await Task.Yield();

            if (!permanently && registerHistory)
            {
                App.HistoryWrapper.AddHistory(history);
            }
            var itemsDeleted = history?.Source.Count() ?? 0;

            source.ForEach(x => App.JumpList.RemoveFolder(x.Path)); // Remove items from jump list

            banner.Remove();
            sw.Stop();

            PostBannerHelpers.PostBanner_Delete(source, returnStatus, permanently, token.IsCancellationRequested, itemsDeleted);

            return returnStatus;
        }

        public async Task<ReturnResult> DeleteItemAsync(IStorageItemWithPath source, bool showDialog, bool permanently, bool registerHistory)
        {
            return await DeleteItemsAsync(source.CreateEnumerable(), showDialog, permanently, registerHistory);
        }

        public async Task<ReturnResult> DeleteItemsAsync(IEnumerable<IStorageItem> source, bool showDialog, bool permanently, bool registerHistory)
        {
            return await DeleteItemsAsync(source.Select((item) => item.FromStorageItem()), showDialog, permanently, registerHistory);
        }

        public async Task<ReturnResult> DeleteItemAsync(IStorageItem source, bool showDialog, bool permanently, bool registerHistory)
        {
            return await DeleteItemAsync(source.FromStorageItem(), showDialog, permanently, registerHistory);
        }

        #endregion Delete

        #region Restore

        public async Task<ReturnResult> RestoreItemFromTrashAsync(IStorageItem source, string destination, bool registerHistory)
        {
            return await RestoreItemFromTrashAsync(source.FromStorageItem(), destination, registerHistory);
        }

        public async Task<ReturnResult> RestoreItemsFromTrashAsync(IEnumerable<IStorageItem> source, IEnumerable<string> destination, bool registerHistory)
        {
            return await RestoreItemsFromTrashAsync(source.Select((item) => item.FromStorageItem()), destination, registerHistory);
        }

        public async Task<ReturnResult> RestoreItemFromTrashAsync(IStorageItemWithPath source, string destination, bool registerHistory)
        {
            return await RestoreItemsFromTrashAsync(source.CreateEnumerable(), destination.CreateEnumerable(), registerHistory);
        }

        public async Task<ReturnResult> RestoreItemsFromTrashAsync(IEnumerable<IStorageItemWithPath> source, IEnumerable<string> destination, bool registerHistory)
        {
            source = await source.ToListAsync();
            destination = await destination.ToListAsync();

            var returnStatus = ReturnResult.InProgress;
            var errorCode = new Progress<FileSystemStatusCode>();
            errorCode.ProgressChanged += (s, e) => returnStatus = returnStatus < ReturnResult.Failed ? e.ToStatus() : returnStatus;

            var sw = new Stopwatch();
            sw.Start();

            IStorageHistory history = await filesystemOperations.RestoreItemsFromTrashAsync((IList<IStorageItemWithPath>)source, (IList<string>)destination, null, errorCode, cancellationToken);
            await Task.Yield();

            if (registerHistory && source.Any((item) => !string.IsNullOrWhiteSpace(item.Path)))
            {
                App.HistoryWrapper.AddHistory(history);
            }
            int itemsMoved = history?.Source.Count() ?? 0;

            sw.Stop();

            return returnStatus;
        }

        #endregion Restore

        public async Task<ReturnResult> PerformOperationTypeAsync(DataPackageOperation operation,
                                                                  DataPackageView packageView,
                                                                  string destination,
                                                                  bool showDialog,
                                                                  bool registerHistory,
                                                                  bool isTargetExecutable = false)
        {
            try
            {
                if (destination == null)
                {
                    return default;
                }
                if (destination.StartsWith(CommonPaths.RecycleBinPath, StringComparison.Ordinal))
                {
                    showDialog |= UserSettingsService.PreferencesSettingsService.ShowConfirmDeleteDialog;
                    return await RecycleItemsFromClipboard(packageView, destination, showDialog, registerHistory);
                }
                else if (operation.HasFlag(DataPackageOperation.Copy))
                {
                    return await CopyItemsFromClipboard(packageView, destination, showDialog, registerHistory);
                }
                else if (operation.HasFlag(DataPackageOperation.Move))
                {
                    return await MoveItemsFromClipboard(packageView, destination, showDialog, registerHistory);
                }
                else if (operation.HasFlag(DataPackageOperation.Link))
                {
                    // Open with piggybacks off of the link operation, since there isn't one for it
                    if (isTargetExecutable)
                    {
                        var handledByFtp = await CheckDragNeedsFulltrust(packageView);
                        if (!handledByFtp)
                        {
                            var items = await GetDraggedStorageItems(packageView);
                            NavigationHelpers.OpenItemsWithExecutable(associatedInstance, items, destination);
                        }
                        return ReturnResult.Success;
                    }
                    else
                    {
                        return await CreateShortcutFromClipboard(packageView, destination, showDialog, registerHistory);
                    }
                }
                else if (operation.HasFlag(DataPackageOperation.None))
                {
                    return await CopyItemsFromClipboard(packageView, destination, showDialog, registerHistory);
                }
                else
                {
                    return default;
                }
            }
            finally
            {
                packageView.ReportOperationCompleted(operation);
            }
        }

        #region Copy

        public async Task<ReturnResult> CopyItemsAsync(IEnumerable<IStorageItem> source, IEnumerable<string> destination, bool showDialog, bool registerHistory)
        {
            return await CopyItemsAsync(source.Select((item) => item.FromStorageItem()), destination, showDialog, registerHistory);
        }

        public async Task<ReturnResult> CopyItemAsync(IStorageItem source, string destination, bool showDialog, bool registerHistory)
        {
            return await CopyItemAsync(source.FromStorageItem(), destination, showDialog, registerHistory);
        }

        public async Task<ReturnResult> CopyItemsAsync(IEnumerable<IStorageItemWithPath> source, IEnumerable<string> destination, bool showDialog, bool registerHistory)
        {
            source = await source.ToListAsync();
            destination = await destination.ToListAsync();

            var returnStatus = ReturnResult.InProgress;

            var banner = PostBannerHelpers.PostBanner_Copy(source, destination, returnStatus, false, 0);
            banner.ErrorCode.ProgressChanged += (s, e) => returnStatus = returnStatus < ReturnResult.Failed ? e.ToStatus() : returnStatus;

            var token = banner.CancellationToken;

            var (collisions, cancelOperation, itemsResult) = await GetCollision(FilesystemOperationType.Copy, source, destination, showDialog);

            if (cancelOperation)
            {
                banner.Remove();
                return ReturnResult.Cancelled;
            }

            var sw = new Stopwatch();
            sw.Start();

            itemManipulationModel?.ClearSelection();

            IStorageHistory history = await filesystemOperations.CopyItemsAsync((IList<IStorageItemWithPath>)source, (IList<string>)destination, collisions, banner.Progress, banner.ErrorCode, token);
            ((IProgress<float>)banner.Progress).Report(100.0f);
            await Task.Yield();

            if (registerHistory && history != null && source.Any((item) => !string.IsNullOrWhiteSpace(item.Path)))
            {
                foreach (var item in history.Source.Zip(history.Destination, (k, v) => new { Key = k, Value = v }).ToDictionary(k => k.Key, v => v.Value))
                {
                    foreach (var item2 in itemsResult)
                    {
                        if (!string.IsNullOrEmpty(item2.CustomName) && item2.SourcePath == item.Key.Path)
                        {
                            var renameHistory = await filesystemOperations.RenameAsync(item.Value, item2.CustomName, NameCollisionOption.FailIfExists, banner.ErrorCode, token);

                            history.Destination[history.Source.IndexOf(item.Key)] = renameHistory.Destination[0];
                        }
                    }
                }
                App.HistoryWrapper.AddHistory(history);
            }
            var itemsCopied = history?.Source.Count() ?? 0;

            banner.Remove();
            sw.Stop();

            PostBannerHelpers.PostBanner_Copy(source, destination, returnStatus, token.IsCancellationRequested, itemsCopied);

            return returnStatus;
        }

        public async Task<ReturnResult> CopyItemAsync(IStorageItemWithPath source, string destination, bool showDialog, bool registerHistory)
        {
            return await CopyItemsAsync(source.CreateEnumerable(), destination.CreateEnumerable(), showDialog, registerHistory);
        }

        public async Task<ReturnResult> CopyItemsFromClipboard(DataPackageView packageView, string destination, bool showDialog, bool registerHistory)
        {
            var handledByFtp = await Filesystem.FilesystemHelpers.CheckDragNeedsFulltrust(packageView);
            var source = await Filesystem.FilesystemHelpers.GetDraggedStorageItems(packageView);

            if (handledByFtp)
            {
                var connection = await ServiceConnection;
                if (connection != null)
                {
                    var (status, response) = await connection.SendMessageForResponseAsync(new ValueSet() {
                        { "Arguments", "FileOperation" },
                        { "fileop", "DragDrop" },
                        { "droppath", associatedInstance.FilesystemViewModel.WorkingDirectory } });
                    return (status == AppServiceResponseStatus.Success && response.Get("Success", false)) ? ReturnResult.Success : ReturnResult.Failed;
                }
                return ReturnResult.Failed;
            }

            if (!source.IsEmpty())
            {
                ReturnResult returnStatus = ReturnResult.InProgress;

                var destinations = new List<string>();
                List<ShellFileItem> binItems = null;
                foreach (var item in source)
                {
                    if (recycleBinHelpers.IsPathUnderRecycleBin(item.Path))
                    {
                        binItems ??= await recycleBinHelpers.EnumerateRecycleBin();
                        if (!binItems.IsEmpty()) // Might still be null because we're deserializing the list from Json
                        {
                            var matchingItem = binItems.FirstOrDefault(x => x.RecyclePath == item.Path); // Get original file name
                            destinations.Add(PathNormalization.Combine(destination, matchingItem?.FileName ?? item.Name));
                        }
                    }
                    else
                    {
                        destinations.Add(PathNormalization.Combine(destination, item.Name));
                    }
                }

                returnStatus = await CopyItemsAsync(source, destinations, showDialog, registerHistory);

                return returnStatus;
            }

            if (packageView.Contains(StandardDataFormats.Bitmap))
            {
                try
                {
                    var imgSource = await packageView.GetBitmapAsync();
                    using var imageStream = await imgSource.OpenReadAsync();
                    var folder = await StorageFileExtensions.DangerousGetFolderFromPathAsync(destination);
                    // Set the name of the file to be the current time and date
                    var file = await folder.CreateFileAsync($"{DateTime.Now:mm-dd-yy-HHmmss}.png", CreationCollisionOption.GenerateUniqueName);

                    SoftwareBitmap softwareBitmap;

                    // Create the decoder from the stream
                    BitmapDecoder decoder = await BitmapDecoder.CreateAsync(imageStream);

                    // Get the SoftwareBitmap representation of the file
                    softwareBitmap = await decoder.GetSoftwareBitmapAsync();

                    await Helpers.BitmapHelper.SaveSoftwareBitmapToFile(softwareBitmap, file, BitmapEncoder.PngEncoderId);
                    return ReturnResult.Success;
                }
                catch (Exception)
                {
                    return ReturnResult.UnknownException;
                }
            }

            // Happens if you copy some text and then you Ctrl+V in Files
            return ReturnResult.BadArgumentException;
        }

        #endregion Copy

        #region Move

        public async Task<ReturnResult> MoveItemsAsync(IEnumerable<IStorageItem> source, IEnumerable<string> destination, bool showDialog, bool registerHistory)
        {
            return await MoveItemsAsync(source.Select((item) => item.FromStorageItem()), destination, showDialog, registerHistory);
        }

        public async Task<ReturnResult> MoveItemAsync(IStorageItem source, string destination, bool showDialog, bool registerHistory)
        {
            return await MoveItemAsync(source.FromStorageItem(), destination, showDialog, registerHistory);
        }

        public async Task<ReturnResult> MoveItemsAsync(IEnumerable<IStorageItemWithPath> source, IEnumerable<string> destination, bool showDialog, bool registerHistory)
        {
            source = await source.ToListAsync();
            destination = await destination.ToListAsync();

            var returnStatus = ReturnResult.InProgress;

            var sourceDir = PathNormalization.GetParentDir(source.FirstOrDefault()?.Path);
            var destinationDir = PathNormalization.GetParentDir(destination.FirstOrDefault());

            var banner = PostBannerHelpers.PostBanner_Move(source, destination, returnStatus, false, 0);
            banner.ErrorCode.ProgressChanged += (s, e) => returnStatus = returnStatus < ReturnResult.Failed ? e.ToStatus() : returnStatus;

            var token = banner.CancellationToken;

            var (collisions, cancelOperation, itemsResult) = await GetCollision(FilesystemOperationType.Move, source, destination, showDialog);

            if (cancelOperation)
            {
                banner.Remove();
                return ReturnResult.Cancelled;
            }

            var sw = new Stopwatch();
            sw.Start();

            itemManipulationModel?.ClearSelection();

            IStorageHistory history = await filesystemOperations.MoveItemsAsync((IList<IStorageItemWithPath>)source, (IList<string>)destination, collisions, banner.Progress, banner.ErrorCode, token);
            ((IProgress<float>)banner.Progress).Report(100.0f);
            await Task.Yield();

            if (registerHistory && history != null && source.Any((item) => !string.IsNullOrWhiteSpace(item.Path)))
            {
                foreach (var item in history.Source.Zip(history.Destination, (k, v) => new { Key = k, Value = v }).ToDictionary(k => k.Key, v => v.Value))
                {
                    foreach (var item2 in itemsResult)
                    {
                        if (!string.IsNullOrEmpty(item2.CustomName) && item2.SourcePath == item.Key.Path)
                        {
                            var renameHistory = await filesystemOperations.RenameAsync(item.Value, item2.CustomName, NameCollisionOption.FailIfExists, banner.ErrorCode, token);

                            history.Destination[history.Source.IndexOf(item.Key)] = renameHistory.Destination[0];
                        }
                    }
                }
                App.HistoryWrapper.AddHistory(history);
            }
            int itemsMoved = history?.Source.Count() ?? 0;

            source.ForEach(x => App.JumpList.RemoveFolder(x.Path)); // Remove items from jump list

            banner.Remove();
            sw.Stop();

            PostBannerHelpers.PostBanner_Move(source, destination, returnStatus, token.IsCancellationRequested, itemsMoved);

            return returnStatus;
        }

        public async Task<ReturnResult> MoveItemAsync(IStorageItemWithPath source, string destination, bool showDialog, bool registerHistory)
        {
            return await MoveItemsAsync(source.CreateEnumerable(), destination.CreateEnumerable(), showDialog, registerHistory);
        }

        public async Task<ReturnResult> MoveItemsFromClipboard(DataPackageView packageView, string destination, bool showDialog, bool registerHistory)
        {
            if (!HasDraggedStorageItems(packageView))
            {
                // Happens if you copy some text and then you Ctrl+V in Files
                return ReturnResult.BadArgumentException;
            }

            var handledByFtp = await Filesystem.FilesystemHelpers.CheckDragNeedsFulltrust(packageView);
            var source = await Filesystem.FilesystemHelpers.GetDraggedStorageItems(packageView);

            if (handledByFtp)
            {
                // Not supported
                return ReturnResult.Failed;
            }

            ReturnResult returnStatus = ReturnResult.InProgress;

            var destinations = new List<string>();
            List<ShellFileItem> binItems = null;
            foreach (var item in source)
            {
                if (recycleBinHelpers.IsPathUnderRecycleBin(item.Path))
                {
                    binItems ??= await recycleBinHelpers.EnumerateRecycleBin();
                    if (!binItems.IsEmpty()) // Might still be null because we're deserializing the list from Json
                    {
                        var matchingItem = binItems.FirstOrDefault(x => x.RecyclePath == item.Path); // Get original file name
                        destinations.Add(PathNormalization.Combine(destination, matchingItem?.FileName ?? item.Name));
                    }
                }
                else
                {
                    destinations.Add(PathNormalization.Combine(destination, item.Name));
                }
            }

            returnStatus = await MoveItemsAsync(source, destinations, showDialog, registerHistory);

            return returnStatus;
        }

        #endregion Move

        #region Rename

        public async Task<ReturnResult> RenameAsync(IStorageItem source, string newName, NameCollisionOption collision, bool registerHistory)
        {
            return await RenameAsync(source.FromStorageItem(), newName, collision, registerHistory);
        }

        public async Task<ReturnResult> RenameAsync(IStorageItemWithPath source, string newName, NameCollisionOption collision, bool registerHistory)
        {
            var returnStatus = ReturnResult.InProgress;
            var errorCode = new Progress<FileSystemStatusCode>();
            errorCode.ProgressChanged += (s, e) => returnStatus = returnStatus < ReturnResult.Failed ? e.ToStatus() : returnStatus;

            IStorageHistory history = null;

            switch (source.ItemType)
            {
                case FilesystemItemType.Directory:
                    history = await filesystemOperations.RenameAsync(source, newName, collision, errorCode, cancellationToken);
                    break;

                case FilesystemItemType.File:

                    /* Only prompt user when extension has changed,
                       not when file name has changed */
                    if (Path.GetExtension(source.Path) != Path.GetExtension(newName))
                    {
                        var yesSelected = await DialogDisplayHelper.ShowDialogAsync("RenameFileDialogTitle".GetLocalized(), "RenameFileDialog/Text".GetLocalized(), "Yes".GetLocalized(), "No".GetLocalized());
                        if (yesSelected)
                        {
                            history = await filesystemOperations.RenameAsync(source, newName, collision, errorCode, cancellationToken);
                            break;
                        }

                        break;
                    }

                    history = await filesystemOperations.RenameAsync(source, newName, collision, errorCode, cancellationToken);
                    break;

                default:
                    history = await filesystemOperations.RenameAsync(source, newName, collision, errorCode, cancellationToken);
                    break;
            }

            if (registerHistory && !string.IsNullOrWhiteSpace(source.Path))
            {
                App.HistoryWrapper.AddHistory(history);
            }

            App.JumpList.RemoveFolder(source.Path); // Remove items from jump list

            await Task.Yield();
            return returnStatus;
        }

        #endregion Rename

        public async Task<ReturnResult> CreateShortcutFromClipboard(DataPackageView packageView, string destination, bool showDialog, bool registerHistory)
        {
            if (!HasDraggedStorageItems(packageView))
            {
                // Happens if you copy some text and then you Ctrl+V in Files
                return ReturnResult.BadArgumentException;
            }

            var handledByFtp = await Filesystem.FilesystemHelpers.CheckDragNeedsFulltrust(packageView);
            var source = await Filesystem.FilesystemHelpers.GetDraggedStorageItems(packageView);

            if (handledByFtp)
            {
                // Not supported
                return ReturnResult.Failed;
            }

            var returnStatus = ReturnResult.InProgress;
            var errorCode = new Progress<FileSystemStatusCode>();
            errorCode.ProgressChanged += (s, e) => returnStatus = returnStatus < ReturnResult.Failed ? e.ToStatus() : returnStatus;

            source = source.Where(x => !string.IsNullOrEmpty(x.Path));
            var dest = source.Select(x => Path.Combine(destination,
                string.Format("ShortcutCreateNewSuffix".GetLocalized(), x.Name) + ".lnk"));

            source = await source.ToListAsync();
            dest = await dest.ToListAsync();

            var history = await filesystemOperations.CreateShortcutItemsAsync((IList<IStorageItemWithPath>)source, (IList<string>)dest, null, errorCode, cancellationToken);

            if (registerHistory)
            {
                App.HistoryWrapper.AddHistory(history);
            }

            await Task.Yield();
            return returnStatus;
        }

        public async Task<ReturnResult> RecycleItemsFromClipboard(DataPackageView packageView, string destination, bool showDialog, bool registerHistory)
        {
            if (!HasDraggedStorageItems(packageView))
            {
                // Happens if you copy some text and then you Ctrl+V in Files
                return ReturnResult.BadArgumentException;
            }

            var handledByFtp = await FilesystemHelpers.CheckDragNeedsFulltrust(packageView);
            var source = await FilesystemHelpers.GetDraggedStorageItems(packageView);

            if (handledByFtp)
            {
                // Not supported
                return ReturnResult.Failed;
            }

            ReturnResult returnStatus = ReturnResult.InProgress;

            source = source.Where(x => !recycleBinHelpers.IsPathUnderRecycleBin(x.Path)); // Can't recycle items already in recyclebin
            returnStatus = await DeleteItemsAsync(source, showDialog, false, registerHistory);

            return returnStatus;
        }

        #endregion IFilesystemHelpers

        private static async Task<(List<FileNameConflictResolveOptionType> collisions, bool cancelOperation, IEnumerable<IFileSystemDialogConflictItemViewModel>)> GetCollision(FilesystemOperationType operationType, IEnumerable<IStorageItemWithPath> source, IEnumerable<string> destination, bool forceDialog)
        {
            var incomingItems = new List<BaseFileSystemDialogItemViewModel>();
            var conflictingItems = new List<BaseFileSystemDialogItemViewModel>();
            var collisions = new Dictionary<string, FileNameConflictResolveOptionType>();

            foreach (var item in source.Zip(destination, (src, dest, index) => new { src, dest, index }))
            {
                var itemPathOrName = string.IsNullOrEmpty(item.src.Path) ? item.src.Item.Name : item.src.Path;
                incomingItems.Add(new FileSystemDialogConflictItemViewModel() { ConflictResolveOption = FileNameConflictResolveOptionType.None, SourcePath = itemPathOrName, DestinationPath = item.dest, DestinationDisplayName = Path.GetFileName(item.dest) });
                if (collisions.ContainsKey(incomingItems.ElementAt(item.index).SourcePath))
                {
                    // Something strange happened, log
                    App.Logger.Warn($"Duplicate key when resolving conflicts: {incomingItems.ElementAt(item.index).SourcePath}, {item.src.Name}\n" +
                        $"Source: {string.Join(", ", source.Select(x => string.IsNullOrEmpty(x.Path) ? x.Item.Name : x.Path))}");
                }
                collisions.AddIfNotPresent(incomingItems.ElementAt(item.index).SourcePath, FileNameConflictResolveOptionType.GenerateNewName);

                // Assume GenerateNewName when source and destination are the same
                if (string.IsNullOrEmpty(item.src.Path) || item.src.Path != item.dest)
                {
                    if (StorageHelpers.Exists(item.dest)) // Same item names in both directories
                    {
                        (incomingItems[item.index] as FileSystemDialogConflictItemViewModel)!.ConflictResolveOption = FileNameConflictResolveOptionType.GenerateNewName;
                        conflictingItems.Add(incomingItems.ElementAt(item.index));
                    }
                }
            }

            IEnumerable<IFileSystemDialogConflictItemViewModel>? itemsResult = null;

            var mustResolveConflicts = !conflictingItems.IsEmpty();
            if (mustResolveConflicts || forceDialog)
            {
                var dialogService = Ioc.Default.GetRequiredService<IDialogService>();

                var dialogViewModel = FileSystemDialogViewModel.GetDialogViewModel(
                    new() { ConflictsExist = mustResolveConflicts },
                    (false, false),
                    operationType,
                    incomingItems.Except(conflictingItems).ToList(), // TODO: Could be optimized
                    conflictingItems);

                var result = await dialogService.ShowDialogAsync(dialogViewModel);
                itemsResult = dialogViewModel.GetItemsResult();
                if (mustResolveConflicts) // If there were conflicts, result buttons are different
                {
                    if (result != DialogResult.Primary) // Operation was cancelled
                    {
                        return (new(), true, itemsResult);
                    }
                }

                collisions.Clear();
                foreach (var item in itemsResult)
                {
                    collisions.AddIfNotPresent(item.SourcePath, item.ConflictResolveOption);
                }
            }

            // Since collisions are scrambled, we need to sort them PATH--PATH
            List<FileNameConflictResolveOptionType> newCollisions = new List<FileNameConflictResolveOptionType>();

            foreach (var src in source)
            {
                var itemPathOrName = string.IsNullOrEmpty(src.Path) ? src.Item.Name : src.Path;
                var match = collisions.SingleOrDefault(x => x.Key == itemPathOrName);
                if (match.Key != null)
                {
                    newCollisions.Add(match.Value);
                }
                else
                {
                    newCollisions.Add(FileNameConflictResolveOptionType.Skip);
                }
            }

            return (newCollisions, false, itemsResult ?? new List<IFileSystemDialogConflictItemViewModel>());
        }

        #region Public Helpers

        public static bool HasDraggedStorageItems(DataPackageView packageView)
        {
            return packageView != null && (packageView.Contains(StandardDataFormats.StorageItems) || (packageView.Properties.TryGetValue("FileDrop", out _)));
        }

        public static async Task<bool> CheckDragNeedsFulltrust(DataPackageView packageView)
        {
            if (packageView.Contains(StandardDataFormats.StorageItems))
            {
                try
                {
                    _ = await packageView.GetStorageItemsAsync();
                    return false;
                }
                catch (Exception ex) when ((uint)ex.HResult == 0x80040064 || (uint)ex.HResult == 0x8004006A)
                {
                    return true;
                }
                catch (Exception ex)
                {
                    App.Logger.Warn(ex, ex.Message);
                    return false;
                }
            }
            return false;
        }

        public static async Task<IEnumerable<IStorageItemWithPath>> GetDraggedStorageItems(DataPackageView packageView)
        {
            var itemsList = new List<IStorageItemWithPath>();
            if (packageView.Contains(StandardDataFormats.StorageItems))
            {
                try
                {
                    var source = await packageView.GetStorageItemsAsync();
                    itemsList.AddRange(source.Select(x => x.FromStorageItem()));
                }
                catch (Exception ex) when ((uint)ex.HResult == 0x80040064 || (uint)ex.HResult == 0x8004006A)
                {
                    return itemsList;
                }
                catch (Exception ex)
                {
                    App.Logger.Warn(ex, ex.Message);
                    return itemsList;
                }
            }
            if (packageView.Properties.TryGetValue("FileDrop", out var data))
            {
                if (data is List<IStorageItemWithPath> source)
                {
                    itemsList.AddRange(source);
                }
            }
            itemsList = itemsList.DistinctBy(x => string.IsNullOrEmpty(x.Path) ? x.Item.Name : x.Path).ToList();
            return itemsList;
        }

        public static string FilterRestrictedCharacters(string input)
        {
            int invalidCharIndex;
            while ((invalidCharIndex = input.IndexOfAny(RestrictedCharacters)) >= 0)
            {
                input = input.Remove(invalidCharIndex, 1);
            }
            return input;
        }

        public static bool ContainsRestrictedCharacters(string input)
        {
            return input.IndexOfAny(RestrictedCharacters) >= 0;
        }

        public static bool ContainsRestrictedFileName(string input)
        {
            foreach (string name in RestrictedFileNames)
            {
                if (input.StartsWith(name, StringComparison.OrdinalIgnoreCase) && (input.Length == name.Length || input[name.Length] == '.'))
                    return true;
            }

            return false;
        }

        #endregion Public Helpers

        #region IDisposable

        public void Dispose()
        {
            filesystemOperations?.Dispose();

            associatedInstance = null;
            filesystemOperations = null;
            recycleBinHelpers = null;
        }

        #endregion IDisposable
    }
}