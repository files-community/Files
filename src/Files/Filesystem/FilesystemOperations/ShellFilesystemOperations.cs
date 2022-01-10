using Files.Common;
using Files.Enums;
using Files.Extensions;
using Files.Filesystem.FilesystemHistory;
using Files.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
using Windows.Storage;

namespace Files.Filesystem
{
    public class ShellFilesystemOperations : IFilesystemOperations
    {
        #region Private Members

        private IShellPage associatedInstance;

        private FilesystemOperations filesystemOperations;

        private RecycleBinHelpers recycleBinHelpers;

        #endregion Private Members

        #region Constructor

        public ShellFilesystemOperations(IShellPage associatedInstance)
        {
            this.associatedInstance = associatedInstance;
            filesystemOperations = new FilesystemOperations(associatedInstance);
            recycleBinHelpers = new RecycleBinHelpers();
        }

        #endregion Constructor

        public async Task<IStorageHistory> CopyAsync(IStorageItem source, string destination, NameCollisionOption collision, IProgress<float> progress, IProgress<FileSystemStatusCode> errorCode, CancellationToken cancellationToken)
        {
            return await CopyAsync(source.FromStorageItem(),
                                                    destination,
                                                    collision,
                                                    progress,
                                                    errorCode,
                                                    cancellationToken);
        }

        public async Task<IStorageHistory> CopyAsync(IStorageItemWithPath source, string destination, NameCollisionOption collision, IProgress<float> progress, IProgress<FileSystemStatusCode> errorCode, CancellationToken cancellationToken)
        {
            return await CopyItemsAsync(source.CreateList(), destination.CreateList(), collision.ConvertBack().CreateList(), progress, errorCode, cancellationToken);
        }

        public async Task<IStorageHistory> CopyItemsAsync(IEnumerable<IStorageItem> source, IEnumerable<string> destination, IEnumerable<FileNameConflictResolveOptionType> collisions, IProgress<float> progress, IProgress<FileSystemStatusCode> errorCode, CancellationToken cancellationToken)
        {
            return await CopyItemsAsync(source.Select((item) => item.FromStorageItem()), destination, collisions, progress, errorCode, cancellationToken);
        }

        public async Task<IStorageHistory> CopyItemsAsync(IEnumerable<IStorageItemWithPath> source, IEnumerable<string> destination, IEnumerable<FileNameConflictResolveOptionType> collisions, IProgress<float> progress, IProgress<FileSystemStatusCode> errorCode, CancellationToken cancellationToken)
        {
            var connection = await AppServiceConnectionHelper.Instance;
            if (connection == null || source.Any(x => string.IsNullOrWhiteSpace(x.Path) || x.Path.StartsWith(@"\\?\", StringComparison.Ordinal)) || destination.Any(x => string.IsNullOrWhiteSpace(x) || x.StartsWith(@"\\?\", StringComparison.Ordinal) || FtpHelpers.IsFtpPath(x)))
            {
                // Fallback to builtin file operations
                return await filesystemOperations.CopyItemsAsync(source, destination, collisions, progress, errorCode, cancellationToken);
            }

            var sourceNoSkip = source.Zip(collisions, (src, coll) => new { src, coll }).Where(item => item.coll != FileNameConflictResolveOptionType.Skip).Select(item => item.src);
            var destinationNoSkip = destination.Zip(collisions, (src, coll) => new { src, coll }).Where(item => item.coll != FileNameConflictResolveOptionType.Skip).Select(item => item.src);
            var collisionsNoSkip = collisions.Where(c => c != FileNameConflictResolveOptionType.Skip);

            var operationID = Guid.NewGuid().ToString();
            using var r = cancellationToken.Register(CancelOperation, operationID, false);

            EventHandler<Dictionary<string, object>> handler = (s, e) => OnProgressUpdated(s, e, operationID, progress);
            connection.RequestReceived += handler;

            var sourceReplace = sourceNoSkip.Zip(collisionsNoSkip, (src, coll) => new { src, coll }).Where(item => item.coll == FileNameConflictResolveOptionType.ReplaceExisting).Select(item => item.src);
            var destinationReplace = destinationNoSkip.Zip(collisionsNoSkip, (src, coll) => new { src, coll }).Where(item => item.coll == FileNameConflictResolveOptionType.ReplaceExisting).Select(item => item.src);
            var sourceRename = sourceNoSkip.Zip(collisionsNoSkip, (src, coll) => new { src, coll }).Where(item => item.coll != FileNameConflictResolveOptionType.ReplaceExisting).Select(item => item.src);
            var destinationRename = destinationNoSkip.Zip(collisionsNoSkip, (src, coll) => new { src, coll }).Where(item => item.coll != FileNameConflictResolveOptionType.ReplaceExisting).Select(item => item.src);

            var result = (FilesystemResult)true;
            var copyResult = new ShellOperationResult();
            if (sourceRename.Any())
            {
                var (status, response) = await connection.SendMessageForResponseAsync(new ValueSet()
                {
                    { "Arguments", "FileOperation" },
                    { "fileop", "CopyItem" },
                    { "operationID", operationID },
                    { "filepath", string.Join('|', sourceRename.Select(s => s.Path)) },
                    { "destpath", string.Join('|', destinationRename) },
                    { "overwrite", false },
                    { "HWND", NativeWinApiHelper.CoreWindowHandle.ToInt64() }
                });
                result &= (FilesystemResult)(status == AppServiceResponseStatus.Success
                    && response.Get("Success", false));
                var shellOpResult = JsonConvert.DeserializeObject<ShellOperationResult>(response.Get("Result", ""));
                copyResult.Items.AddRange(shellOpResult?.Items ?? Enumerable.Empty<ShellOperationItemResult>());
            }
            if (sourceReplace.Any())
            {
                var (status, response) = await connection.SendMessageForResponseAsync(new ValueSet()
                {
                    { "Arguments", "FileOperation" },
                    { "fileop", "CopyItem" },
                    { "operationID", operationID },
                    { "filepath", string.Join('|', sourceReplace.Select(s => s.Path)) },
                    { "destpath", string.Join('|', destinationReplace) },
                    { "overwrite", true },
                    { "HWND", NativeWinApiHelper.CoreWindowHandle.ToInt64() }
                });
                result &= (FilesystemResult)(status == AppServiceResponseStatus.Success
                    && response.Get("Success", false));
                var shellOpResult = JsonConvert.DeserializeObject<ShellOperationResult>(response.Get("Result", ""));
                copyResult.Items.AddRange(shellOpResult?.Items ?? Enumerable.Empty<ShellOperationItemResult>());
            }

            if (connection != null)
            {
                connection.RequestReceived -= handler;
            }

            result &= (FilesystemResult)copyResult.Items.All(x => x.Succeeded);

            if (result)
            {
                progress?.Report(100.0f);
                errorCode?.Report(FileSystemStatusCode.Success);
                var copiedSources = copyResult.Items.Where(x => sourceRename.Select(s => s.Path).Contains(x.Source)).Where(x => x.Succeeded && x.Destination != null && x.Source != x.Destination);
                if (copiedSources.Any())
                {
                    return new StorageHistory(FileOperationType.Copy, copiedSources.Select(x => sourceRename.Single(s => s.Path == x.Source)),
                        copiedSources.Select(item => StorageHelpers.FromPathAndType(item.Destination, sourceRename.Single(s => s.Path == item.Source).ItemType)));
                }
                return null; // Cannot undo overwrite operation
            }
            else
            {
                // Retry failed operations
                var failedSources = copyResult.Items.Where(x => sourceNoSkip.Select(s => s.Path).Contains(x.Source)).Where(x => !x.Succeeded);
                var copyZip = sourceNoSkip.Zip(destinationNoSkip, (src, dest) => new { src, dest }).Zip(collisionsNoSkip, (z1, coll) => new { z1.src, z1.dest, coll });
                return await filesystemOperations.CopyItemsAsync(
                    failedSources.Select(x => copyZip.Single(s => s.src.Path == x.Source).src),
                    failedSources.Select(x => copyZip.Single(s => s.src.Path == x.Source).dest),
                    failedSources.Select(x => copyZip.Single(s => s.src.Path == x.Source).coll), progress, errorCode, cancellationToken);
            }
        }

        public async Task<(IStorageHistory, IStorageItem)> CreateAsync(IStorageItemWithPath source, IProgress<FileSystemStatusCode> errorCode, CancellationToken cancellationToken)
        {
            return await filesystemOperations.CreateAsync(source, errorCode, cancellationToken);
        }

        public async Task<IStorageHistory> CreateShortcutItemsAsync(IEnumerable<IStorageItemWithPath> source, IEnumerable<string> destination, IProgress<float> progress, IProgress<FileSystemStatusCode> errorCode, CancellationToken cancellationToken)
        {
            var createdSources = new List<IStorageItemWithPath>();
            var createdDestination = new List<IStorageItemWithPath>();
            var sourceCount = source.Count();

            var connection = await AppServiceConnectionHelper.Instance;
            if (connection != null)
            {
                var items = source.Zip(destination, (src, dest, index) => new { src, dest, index }).Where(x => !string.IsNullOrEmpty(x.src.Path) && !string.IsNullOrEmpty(x.dest));
                foreach (var item in items)
                {
                    var value = new ValueSet()
                    {
                        { "Arguments", "FileOperation" },
                        { "fileop", "CreateLink" },
                        { "targetpath", item.src.Path },
                        { "arguments", "" },
                        { "workingdir", "" },
                        { "runasadmin", false },
                        { "filepath", item.dest }
                    };
                    var (status, response) = await connection.SendMessageForResponseAsync(value);
                    var success = status == AppServiceResponseStatus.Success && response.Get("Success", false);
                    if (success)
                    {
                        createdSources.Add(item.src);
                        createdDestination.Add(StorageHelpers.FromPathAndType(item.dest, FilesystemItemType.File));
                    }
                    progress?.Report(item.index / (float)sourceCount * 100.0f);
                }
            }

            errorCode?.Report(createdSources.Count == sourceCount ? FileSystemStatusCode.Success : FileSystemStatusCode.Generic);
            return new StorageHistory(FileOperationType.CreateLink, createdSources, createdDestination);
        }

        public async Task<IStorageHistory> DeleteAsync(IStorageItem source, IProgress<float> progress, IProgress<FileSystemStatusCode> errorCode, bool permanently, CancellationToken cancellationToken)
        {
            return await DeleteAsync(source.FromStorageItem(),
                                                      progress,
                                                      errorCode,
                                                      permanently,
                                                      cancellationToken);
        }

        public async Task<IStorageHistory> DeleteAsync(IStorageItemWithPath source, IProgress<float> progress, IProgress<FileSystemStatusCode> errorCode, bool permanently, CancellationToken cancellationToken)
        {
            return await DeleteItemsAsync(source.CreateList(), progress, errorCode, permanently, cancellationToken);
        }

        public async Task<IStorageHistory> DeleteItemsAsync(IEnumerable<IStorageItem> source, IProgress<float> progress, IProgress<FileSystemStatusCode> errorCode, bool permanently, CancellationToken cancellationToken)
        {
            return await DeleteItemsAsync(source.Select((item) => item.FromStorageItem()), progress, errorCode, permanently, cancellationToken);
        }

        public async Task<IStorageHistory> DeleteItemsAsync(IEnumerable<IStorageItemWithPath> source, IProgress<float> progress, IProgress<FileSystemStatusCode> errorCode, bool permanently, CancellationToken cancellationToken)
        {
            var connection = await AppServiceConnectionHelper.Instance;
            if (connection == null || source.Any(x => string.IsNullOrWhiteSpace(x.Path) || x.Path.StartsWith(@"\\?\", StringComparison.Ordinal) || FtpHelpers.IsFtpPath(x.Path)))
            {
                // Fallback to builtin file operations
                return await filesystemOperations.DeleteItemsAsync(source, progress, errorCode, permanently, cancellationToken);
            }

            source = source.DistinctBy(x => x.Path); // #5771
            var deleleFilePaths = source.Select(s => s.Path);

            var deleteFromRecycleBin = source.Any() ? recycleBinHelpers.IsPathUnderRecycleBin(source.ElementAt(0).Path) : false;
            permanently |= deleteFromRecycleBin;

            if (deleteFromRecycleBin)
            {
                // Recycle bin also stores a file starting with $I for each item
                deleleFilePaths = deleleFilePaths.Concat(source.Select(x => Path.Combine(Path.GetDirectoryName(x.Path), Path.GetFileName(x.Path).Replace("$R", "$I", StringComparison.Ordinal)))).Distinct();
            }

            var operationID = Guid.NewGuid().ToString();
            using var r = cancellationToken.Register(CancelOperation, operationID, false);

            EventHandler<Dictionary<string, object>> handler = (s, e) => OnProgressUpdated(s, e, operationID, progress);
            connection.RequestReceived += handler;

            var deleteResult = new ShellOperationResult();
            var (status, response) = await connection.SendMessageForResponseAsync(new ValueSet()
            {
                { "Arguments", "FileOperation" },
                { "fileop", "DeleteItem" },
                { "operationID", operationID },
                { "filepath", string.Join('|', deleleFilePaths) },
                { "permanently", permanently },
                { "HWND", NativeWinApiHelper.CoreWindowHandle.ToInt64() }
            });
            var result = (FilesystemResult)(status == AppServiceResponseStatus.Success
                && response.Get("Success", false));
            var shellOpResult = JsonConvert.DeserializeObject<ShellOperationResult>(response.Get("Result", ""));
            deleteResult.Items.AddRange(shellOpResult?.Items ?? Enumerable.Empty<ShellOperationItemResult>());

            if (connection != null)
            {
                connection.RequestReceived -= handler;
            }

            result &= (FilesystemResult)deleteResult.Items.All(x => x.Succeeded);

            if (result)
            {
                progress?.Report(100.0f);
                errorCode?.Report(FileSystemStatusCode.Success);
                foreach (var item in deleteResult.Items)
                {
                    await associatedInstance.FilesystemViewModel.RemoveFileOrFolderAsync(item.Source);
                }
                var recycledSources = deleteResult.Items.Where(x => x.Succeeded && x.Destination != null && x.Source != x.Destination)
                    .Where(x => source.Select(s => s.Path).Contains(x.Source));
                if (recycledSources.Any())
                {
                    return new StorageHistory(FileOperationType.Recycle, recycledSources.Select(x => source.Single(s => s.Path == x.Source)),
                        recycledSources.Select(item => StorageHelpers.FromPathAndType(item.Destination, source.Single(s => s.Path == item.Source).ItemType)));
                }
                return new StorageHistory(FileOperationType.Delete, source, null);
            }
            else
            {
                // Retry failed operations
                var failedSources = deleteResult.Items
                    .Where(x => !x.Succeeded && x.HRresult != HRESULT.COPYENGINE_E_USER_CANCELLED && x.HRresult != HRESULT.COPYENGINE_E_RECYCLE_BIN_NOT_FOUND)
                    .Where(x => source.Select(s => s.Path).Contains(x.Source));
                return await filesystemOperations.DeleteItemsAsync(
                    failedSources.Select(x => source.Single(s => s.Path == x.Source)), progress, errorCode, permanently, cancellationToken);
            }
        }

        public async Task<IStorageHistory> MoveAsync(IStorageItem source, string destination, NameCollisionOption collision, IProgress<float> progress, IProgress<FileSystemStatusCode> errorCode, CancellationToken cancellationToken)
        {
            return await MoveAsync(source.FromStorageItem(),
                                                    destination,
                                                    collision,
                                                    progress,
                                                    errorCode,
                                                    cancellationToken);
        }

        public async Task<IStorageHistory> MoveAsync(IStorageItemWithPath source, string destination, NameCollisionOption collision, IProgress<float> progress, IProgress<FileSystemStatusCode> errorCode, CancellationToken cancellationToken)
        {
            return await MoveItemsAsync(source.CreateList(), destination.CreateList(), collision.ConvertBack().CreateList(), progress, errorCode, cancellationToken);
        }

        public async Task<IStorageHistory> MoveItemsAsync(IEnumerable<IStorageItem> source, IEnumerable<string> destination, IEnumerable<FileNameConflictResolveOptionType> collisions, IProgress<float> progress, IProgress<FileSystemStatusCode> errorCode, CancellationToken cancellationToken)
        {
            return await MoveItemsAsync(source.Select((item) => item.FromStorageItem()), destination, collisions, progress, errorCode, cancellationToken);
        }

        public async Task<IStorageHistory> MoveItemsAsync(IEnumerable<IStorageItemWithPath> source, IEnumerable<string> destination, IEnumerable<FileNameConflictResolveOptionType> collisions, IProgress<float> progress, IProgress<FileSystemStatusCode> errorCode, CancellationToken cancellationToken)
        {
            var connection = await AppServiceConnectionHelper.Instance;
            if (connection == null || source.Any(x => string.IsNullOrWhiteSpace(x.Path) || x.Path.StartsWith(@"\\?\", StringComparison.Ordinal)) || destination.Any(x => string.IsNullOrWhiteSpace(x) || x.StartsWith(@"\\?\", StringComparison.Ordinal) || FtpHelpers.IsFtpPath(x)))
            {
                // Fallback to builtin file operations
                return await filesystemOperations.MoveItemsAsync(source, destination, collisions, progress, errorCode, cancellationToken);
            }

            var sourceNoSkip = source.Zip(collisions, (src, coll) => new { src, coll }).Where(item => item.coll != FileNameConflictResolveOptionType.Skip).Select(item => item.src);
            var destinationNoSkip = destination.Zip(collisions, (src, coll) => new { src, coll }).Where(item => item.coll != FileNameConflictResolveOptionType.Skip).Select(item => item.src);
            var collisionsNoSkip = collisions.Where(c => c != FileNameConflictResolveOptionType.Skip);

            var operationID = Guid.NewGuid().ToString();
            using var r = cancellationToken.Register(CancelOperation, operationID, false);

            EventHandler<Dictionary<string, object>> handler = (s, e) => OnProgressUpdated(s, e, operationID, progress);
            connection.RequestReceived += handler;

            var sourceReplace = sourceNoSkip.Zip(collisionsNoSkip, (src, coll) => new { src, coll }).Where(item => item.coll == FileNameConflictResolveOptionType.ReplaceExisting).Select(item => item.src);
            var destinationReplace = destinationNoSkip.Zip(collisionsNoSkip, (src, coll) => new { src, coll }).Where(item => item.coll == FileNameConflictResolveOptionType.ReplaceExisting).Select(item => item.src);
            var sourceRename = sourceNoSkip.Zip(collisionsNoSkip, (src, coll) => new { src, coll }).Where(item => item.coll != FileNameConflictResolveOptionType.ReplaceExisting).Select(item => item.src);
            var destinationRename = destinationNoSkip.Zip(collisionsNoSkip, (src, coll) => new { src, coll }).Where(item => item.coll != FileNameConflictResolveOptionType.ReplaceExisting).Select(item => item.src);

            var result = (FilesystemResult)true;
            var moveResult = new ShellOperationResult();
            if (sourceRename.Any())
            {
                var (status, response) = await connection.SendMessageForResponseAsync(new ValueSet()
                {
                    { "Arguments", "FileOperation" },
                    { "fileop", "MoveItem" },
                    { "operationID", operationID },
                    { "filepath", string.Join('|', sourceRename.Select(s => s.Path)) },
                    { "destpath", string.Join('|', destinationRename) },
                    { "overwrite", false },
                    { "HWND", NativeWinApiHelper.CoreWindowHandle.ToInt64() }
                });
                result &= (FilesystemResult)(status == AppServiceResponseStatus.Success
                    && response.Get("Success", false));
                var shellOpResult = JsonConvert.DeserializeObject<ShellOperationResult>(response.Get("Result", ""));
                moveResult.Items.AddRange(shellOpResult?.Items ?? Enumerable.Empty<ShellOperationItemResult>());
            }
            if (sourceReplace.Any())
            {
                var (status, response) = await connection.SendMessageForResponseAsync(new ValueSet()
                {
                    { "Arguments", "FileOperation" },
                    { "fileop", "MoveItem" },
                    { "operationID", operationID },
                    { "filepath", string.Join('|', sourceReplace.Select(s => s.Path)) },
                    { "destpath", string.Join('|', destinationReplace) },
                    { "overwrite", true },
                    { "HWND", NativeWinApiHelper.CoreWindowHandle.ToInt64() }
                });
                result &= (FilesystemResult)(status == AppServiceResponseStatus.Success
                    && response.Get("Success", false));
                var shellOpResult = JsonConvert.DeserializeObject<ShellOperationResult>(response.Get("Result", ""));
                moveResult.Items.AddRange(shellOpResult?.Items ?? Enumerable.Empty<ShellOperationItemResult>());
            }

            if (connection != null)
            {
                connection.RequestReceived -= handler;
            }

            result &= (FilesystemResult)moveResult.Items.All(x => x.Succeeded);

            if (result)
            {
                progress?.Report(100.0f);
                errorCode?.Report(FileSystemStatusCode.Success);
                var movedSources = moveResult.Items.Where(x => sourceRename.Select(s => s.Path).Contains(x.Source)).Where(x => x.Succeeded && x.Destination != null && x.Source != x.Destination);
                if (movedSources.Any())
                {
                    return new StorageHistory(FileOperationType.Move, movedSources.Select(x => sourceRename.Single(s => s.Path == x.Source)),
                        movedSources.Select(item => StorageHelpers.FromPathAndType(item.Destination, sourceRename.Single(s => s.Path == item.Source).ItemType)));
                }
                return null; // Cannot undo overwrite operation
            }
            else
            {
                // Retry failed operations
                var failedSources = moveResult.Items.Where(x => sourceNoSkip.Select(s => s.Path).Contains(x.Source)).Where(x => !x.Succeeded);
                var moveZip = sourceNoSkip.Zip(destinationNoSkip, (src, dest) => new { src, dest }).Zip(collisionsNoSkip, (z1, coll) => new { z1.src, z1.dest, coll });
                return await filesystemOperations.MoveItemsAsync(
                    failedSources.Select(x => moveZip.Single(s => s.src.Path == x.Source).src),
                    failedSources.Select(x => moveZip.Single(s => s.src.Path == x.Source).dest),
                    failedSources.Select(x => moveZip.Single(s => s.src.Path == x.Source).coll), progress, errorCode, cancellationToken);
            }
        }

        public async Task<IStorageHistory> RenameAsync(IStorageItem source, string newName, NameCollisionOption collision, IProgress<FileSystemStatusCode> errorCode, CancellationToken cancellationToken)
        {
            return await RenameAsync(StorageHelpers.FromStorageItem(source), newName, collision, errorCode, cancellationToken);
        }

        public async Task<IStorageHistory> RenameAsync(IStorageItemWithPath source, string newName, NameCollisionOption collision, IProgress<FileSystemStatusCode> errorCode, CancellationToken cancellationToken)
        {
            var connection = await AppServiceConnectionHelper.Instance;
            if (connection == null || string.IsNullOrWhiteSpace(source.Path) || source.Path.StartsWith(@"\\?\", StringComparison.Ordinal) || FtpHelpers.IsFtpPath(source.Path))
            {
                // Fallback to builtin file operations
                return await filesystemOperations.RenameAsync(source, newName, collision, errorCode, cancellationToken);
            }

            var renameResult = new ShellOperationResult();
            var (status, response) = await connection.SendMessageForResponseAsync(new ValueSet()
            {
                { "Arguments", "FileOperation" },
                { "fileop", "RenameItem" },
                { "operationID", Guid.NewGuid().ToString() },
                { "filepath", source.Path },
                { "newName", newName },
                { "overwrite", collision == NameCollisionOption.ReplaceExisting }
            });
            var result = (FilesystemResult)(status == AppServiceResponseStatus.Success
                && response.Get("Success", false));
            var shellOpResult = JsonConvert.DeserializeObject<ShellOperationResult>(response.Get("Result", ""));
            renameResult.Items.AddRange(shellOpResult?.Items ?? Enumerable.Empty<ShellOperationItemResult>());

            result &= (FilesystemResult)renameResult.Items.All(x => x.Succeeded);

            if (result)
            {
                errorCode?.Report(FileSystemStatusCode.Success);
                var renamedSources = renameResult.Items.Where(x => new[] { source }.Select(s => s.Path).Contains(x.Source)).Where(x => x.Succeeded && x.Destination != null && x.Source != x.Destination);
                if (renamedSources.Any())
                {
                    return new StorageHistory(FileOperationType.Rename, source,
                        StorageHelpers.FromPathAndType(renamedSources.Single().Destination, source.ItemType));
                }
                return null; // Cannot undo overwrite operation
            }
            else
            {
                // Retry failed operations
                return await filesystemOperations.RenameAsync(source, newName, collision, errorCode, cancellationToken);
            }
        }

        public async Task<IStorageHistory> RestoreFromTrashAsync(IStorageItem source, string destination, IProgress<float> progress, IProgress<FileSystemStatusCode> errorCode, CancellationToken cancellationToken)
        {
            return await RestoreFromTrashAsync(source.FromStorageItem(), destination, progress, errorCode, cancellationToken);
        }

        public async Task<IStorageHistory> RestoreFromTrashAsync(IStorageItemWithPath source, string destination, IProgress<float> progress, IProgress<FileSystemStatusCode> errorCode, CancellationToken cancellationToken)
        {
            return await RestoreItemsFromTrashAsync(source.CreateList(), destination.CreateList(), progress, errorCode, cancellationToken);
        }

        public async Task<IStorageHistory> RestoreItemsFromTrashAsync(IEnumerable<IStorageItem> source, IEnumerable<string> destination, IProgress<float> progress, IProgress<FileSystemStatusCode> errorCode, CancellationToken cancellationToken)
        {
            return await RestoreItemsFromTrashAsync(source.Select((item) => item.FromStorageItem()), destination, progress, errorCode, cancellationToken);
        }

        public async Task<IStorageHistory> RestoreItemsFromTrashAsync(IEnumerable<IStorageItemWithPath> source, IEnumerable<string> destination, IProgress<float> progress, IProgress<FileSystemStatusCode> errorCode, CancellationToken cancellationToken)
        {
            var connection = await AppServiceConnectionHelper.Instance;
            if (connection == null || source.Any(x => string.IsNullOrWhiteSpace(x.Path) || x.Path.StartsWith(@"\\?\", StringComparison.Ordinal)) || destination.Any(x => string.IsNullOrWhiteSpace(x) || x.StartsWith(@"\\?\", StringComparison.Ordinal) || FtpHelpers.IsFtpPath(x)))
            {
                // Fallback to builtin file operations
                return await filesystemOperations.RestoreItemsFromTrashAsync(source, destination, progress, errorCode, cancellationToken);
            }

            var operationID = Guid.NewGuid().ToString();
            using var r = cancellationToken.Register(CancelOperation, operationID, false);

            EventHandler<Dictionary<string, object>> handler = (s, e) => OnProgressUpdated(s, e, operationID, progress);
            connection.RequestReceived += handler;

            var moveResult = new ShellOperationResult();
            var (status, response) = await connection.SendMessageForResponseAsync(new ValueSet()
            {
                { "Arguments", "FileOperation" },
                { "fileop", "MoveItem" },
                { "operationID", operationID },
                { "filepath", string.Join('|', source.Select(s => s.Path)) },
                { "destpath", string.Join('|', destination) },
                { "overwrite", false },
                { "HWND", NativeWinApiHelper.CoreWindowHandle.ToInt64() }
            });
            var result = (FilesystemResult)(status == AppServiceResponseStatus.Success
                && response.Get("Success", false));
            var shellOpResult = JsonConvert.DeserializeObject<ShellOperationResult>(response.Get("Result", ""));
            moveResult.Items.AddRange(shellOpResult?.Items ?? Enumerable.Empty<ShellOperationItemResult>());

            if (connection != null)
            {
                connection.RequestReceived -= handler;
            }

            result &= (FilesystemResult)moveResult.Items.All(x => x.Succeeded);

            if (result)
            {
                progress?.Report(100.0f);
                errorCode?.Report(FileSystemStatusCode.Success);

                var movedSources = moveResult.Items.Where(x => x.Succeeded && x.Destination != null && x.Source != x.Destination)
                    .Where(x => source.Select(s => s.Path).Contains(x.Source));
                if (movedSources.Any())
                {
                    // Recycle bin also stores a file starting with $I for each item
                    await DeleteItemsAsync(movedSources.Select(src => StorageHelpers.FromPathAndType(
                        Path.Combine(Path.GetDirectoryName(src.Source), Path.GetFileName(src.Source).Replace("$R", "$I", StringComparison.Ordinal)),
                        source.Single(s => s.Path == src.Source).ItemType)), null, null, true, cancellationToken);
                    return new StorageHistory(FileOperationType.Restore, movedSources.Select(x => source.Single(s => s.Path == x.Source)),
                        movedSources.Select(item => StorageHelpers.FromPathAndType(item.Destination, source.Single(s => s.Path == item.Source).ItemType)));
                }
                return null; // Cannot undo overwrite operation
            }
            else
            {
                // Retry failed operations
                var failedSources = moveResult.Items.Where(x => !x.Succeeded).Where(x => source.Select(s => s.Path).Contains(x.Source));
                var moveZip = source.Zip(destination, (src, dest) => new { src, dest });
                return await filesystemOperations.RestoreItemsFromTrashAsync(
                    failedSources.Select(x => moveZip.Single(s => s.src.Path == x.Source).src),
                    failedSources.Select(x => moveZip.Single(s => s.src.Path == x.Source).dest), progress, errorCode, cancellationToken);
            }
        }

        private void OnProgressUpdated(object sender, Dictionary<string, object> message, string currentOperation, IProgress<float> progress)
        {
            if (message.ContainsKey("OperationID"))
            {
                var operationID = (string)message["OperationID"];
                if (operationID == currentOperation)
                {
                    var value = (long)message["Progress"];
                    progress?.Report(value);
                }
            }
        }

        private async void CancelOperation(object operationID)
        {
            var connection = await AppServiceConnectionHelper.Instance;
            if (connection != null)
            {
                await connection.SendMessageAsync(new ValueSet()
                {
                    { "Arguments", "FileOperation" },
                    { "fileop", "CancelOperation" },
                    { "operationID", (string)operationID }
                });
            }
        }

        private struct HRESULT
        {
            public const int S_OK = 0;
            public const int COPYENGINE_E_USER_CANCELLED = -2144927744;
            public const int COPYENGINE_E_RECYCLE_BIN_NOT_FOUND = -2144927686;
        }

        #region IDisposable

        public void Dispose()
        {
            filesystemOperations?.Dispose();

            filesystemOperations = null;
            recycleBinHelpers = null;
            associatedInstance = null;
        }

        #endregion IDisposable
    }
}