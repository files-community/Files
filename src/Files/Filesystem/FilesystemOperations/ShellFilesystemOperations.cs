using Files.Shared;
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

        public async Task<IStorageHistory> CopyItemsAsync(IList<IStorageItem> source, IList<string> destination, IList<FileNameConflictResolveOptionType> collisions, IProgress<float> progress, IProgress<FileSystemStatusCode> errorCode, CancellationToken cancellationToken)
        {
            return await CopyItemsAsync(await source.Select((item) => item.FromStorageItem()).ToListAsync(), destination, collisions, progress, errorCode, cancellationToken);
        }

        public async Task<IStorageHistory> CopyItemsAsync(IList<IStorageItemWithPath> source, IList<string> destination, IList<FileNameConflictResolveOptionType> collisions, IProgress<float> progress, IProgress<FileSystemStatusCode> errorCode, CancellationToken cancellationToken)
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
                var copiedSources = copyResult.Items.Where(x => x.Succeeded && x.Destination != null && x.Source != x.Destination);
                if (copiedSources.Any())
                {
                    var sourceMatch = await copiedSources.Select(x => sourceRename.SingleOrDefault(s => s.Path == x.Source)).Where(x => x != null).ToListAsync();
                    return new StorageHistory(FileOperationType.Copy, 
                        sourceMatch,
                        await copiedSources.Zip(sourceMatch, (rSrc, oSrc) => new { rSrc, oSrc })
                            .Select(item => StorageHelpers.FromPathAndType(item.rSrc.Destination, item.oSrc.ItemType)).ToListAsync());
                }
                return null; // Cannot undo overwrite operation
            }
            else
            {
                // Retry failed operations
                var failedSources = copyResult.Items.Where(x => !x.Succeeded);
                var copyZip = sourceNoSkip.Zip(destinationNoSkip, (src, dest) => new { src, dest }).Zip(collisionsNoSkip, (z1, coll) => new { z1.src, z1.dest, coll });
                var sourceMatch = await failedSources.Select(x => copyZip.SingleOrDefault(s => s.src.Path == x.Source)).Where(x => x != null).ToListAsync();
                return await filesystemOperations.CopyItemsAsync(
                    await sourceMatch.Select(x => x.src).ToListAsync(),
                    await sourceMatch.Select(x => x.dest).ToListAsync(),
                    await sourceMatch.Select(x => x.coll).ToListAsync(), progress, errorCode, cancellationToken);
            }
        }

        public async Task<(IStorageHistory, IStorageItem)> CreateAsync(IStorageItemWithPath source, IProgress<FileSystemStatusCode> errorCode, CancellationToken cancellationToken)
        {
            return await filesystemOperations.CreateAsync(source, errorCode, cancellationToken);
        }

        public async Task<IStorageHistory> CreateShortcutItemsAsync(IList<IStorageItemWithPath> source, IList<string> destination, IProgress<float> progress, IProgress<FileSystemStatusCode> errorCode, CancellationToken cancellationToken)
        {
            var createdSources = new List<IStorageItemWithPath>();
            var createdDestination = new List<IStorageItemWithPath>();

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
                    progress?.Report(item.index / (float)source.Count * 100.0f);
                }
            }

            errorCode?.Report(createdSources.Count == source.Count ? FileSystemStatusCode.Success : FileSystemStatusCode.Generic);
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

        public async Task<IStorageHistory> DeleteItemsAsync(IList<IStorageItem> source, IProgress<float> progress, IProgress<FileSystemStatusCode> errorCode, bool permanently, CancellationToken cancellationToken)
        {
            return await DeleteItemsAsync(await source.Select((item) => item.FromStorageItem()).ToListAsync(), progress, errorCode, permanently, cancellationToken);
        }

        public async Task<IStorageHistory> DeleteItemsAsync(IList<IStorageItemWithPath> source, IProgress<float> progress, IProgress<FileSystemStatusCode> errorCode, bool permanently, CancellationToken cancellationToken)
        {
            var connection = await AppServiceConnectionHelper.Instance;
            if (connection == null || source.Any(x => string.IsNullOrWhiteSpace(x.Path) || x.Path.StartsWith(@"\\?\", StringComparison.Ordinal) || FtpHelpers.IsFtpPath(x.Path)))
            {
                // Fallback to builtin file operations
                return await filesystemOperations.DeleteItemsAsync(source, progress, errorCode, permanently, cancellationToken);
            }

            var deleleFilePaths = source.Select(s => s.Path).Distinct();
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
                var recycledSources = deleteResult.Items.Where(x => x.Succeeded && x.Destination != null && x.Source != x.Destination);
                if (recycledSources.Any())
                {
                    var sourceMatch = await recycledSources.Select(x => source.DistinctBy(x => x.Path).SingleOrDefault(s => s.Path == x.Source)).Where(x => x != null).ToListAsync();
                    return new StorageHistory(FileOperationType.Recycle,
                        sourceMatch,
                        await recycledSources.Zip(sourceMatch, (rSrc, oSrc) => new { rSrc, oSrc })
                            .Select(item => StorageHelpers.FromPathAndType(item.rSrc.Destination, item.oSrc.ItemType)).ToListAsync());
                }
                return new StorageHistory(FileOperationType.Delete, source, null);
            }
            else
            {
                // Retry failed operations
                var failedSources = deleteResult.Items
                    .Where(x => !x.Succeeded && x.HResult != HResult.COPYENGINE_E_USER_CANCELLED && x.HResult != HResult.COPYENGINE_E_RECYCLE_BIN_NOT_FOUND);
                return await filesystemOperations.DeleteItemsAsync(
                    await failedSources.Select(x => source.DistinctBy(x => x.Path).SingleOrDefault(s => s.Path == x.Source)).Where(x => x != null).ToListAsync(), progress, errorCode, permanently, cancellationToken);
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

        public async Task<IStorageHistory> MoveItemsAsync(IList<IStorageItem> source, IList<string> destination, IList<FileNameConflictResolveOptionType> collisions, IProgress<float> progress, IProgress<FileSystemStatusCode> errorCode, CancellationToken cancellationToken)
        {
            return await MoveItemsAsync(await source.Select((item) => item.FromStorageItem()).ToListAsync(), destination, collisions, progress, errorCode, cancellationToken);
        }

        public async Task<IStorageHistory> MoveItemsAsync(IList<IStorageItemWithPath> source, IList<string> destination, IList<FileNameConflictResolveOptionType> collisions, IProgress<float> progress, IProgress<FileSystemStatusCode> errorCode, CancellationToken cancellationToken)
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
                var movedSources = moveResult.Items.Where(x => x.Succeeded && x.Destination != null && x.Source != x.Destination);
                if (movedSources.Any())
                {
                    var sourceMatch = await movedSources.Select(x => sourceRename.SingleOrDefault(s => s.Path == x.Source)).Where(x => x != null).ToListAsync();
                    return new StorageHistory(FileOperationType.Move,
                        sourceMatch,
                        await movedSources.Zip(sourceMatch, (rSrc, oSrc) => new { rSrc, oSrc })
                            .Select(item => StorageHelpers.FromPathAndType(item.rSrc.Destination, item.oSrc.ItemType)).ToListAsync());
                }
                return null; // Cannot undo overwrite operation
            }
            else
            {
                // Retry failed operations
                var failedSources = moveResult.Items.Where(x => !x.Succeeded);
                var moveZip = sourceNoSkip.Zip(destinationNoSkip, (src, dest) => new { src, dest }).Zip(collisionsNoSkip, (z1, coll) => new { z1.src, z1.dest, coll });
                var sourceMatch = await failedSources.Select(x => moveZip.SingleOrDefault(s => s.src.Path == x.Source)).Where(x => x != null).ToListAsync();
                return await filesystemOperations.MoveItemsAsync(
                    await sourceMatch.Select(x => x.src).ToListAsync(),
                    await sourceMatch.Select(x => x.dest).ToListAsync(),
                    await sourceMatch.Select(x => x.coll).ToListAsync(), progress, errorCode, cancellationToken);
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
                var renamedSources = renameResult.Items.Where(x => x.Succeeded && x.Destination != null && x.Source != x.Destination)
                    .Where(x => new[] { source }.Select(s => s.Path).Contains(x.Source));
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

        public async Task<IStorageHistory> RestoreItemsFromTrashAsync(IList<IStorageItem> source, IList<string> destination, IProgress<float> progress, IProgress<FileSystemStatusCode> errorCode, CancellationToken cancellationToken)
        {
            return await RestoreItemsFromTrashAsync(await source.Select((item) => item.FromStorageItem()).ToListAsync(), destination, progress, errorCode, cancellationToken);
        }

        public async Task<IStorageHistory> RestoreItemsFromTrashAsync(IList<IStorageItemWithPath> source, IList<string> destination, IProgress<float> progress, IProgress<FileSystemStatusCode> errorCode, CancellationToken cancellationToken)
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

                var movedSources = moveResult.Items.Where(x => x.Succeeded && x.Destination != null && x.Source != x.Destination);
                if (movedSources.Any())
                {
                    var sourceMatch = await movedSources.Select(x => source.SingleOrDefault(s => s.Path == x.Source)).Where(x => x != null).ToListAsync();
                    // Recycle bin also stores a file starting with $I for each item
                    await DeleteItemsAsync(await movedSources.Zip(sourceMatch, (rSrc, oSrc) => new { rSrc, oSrc })
                        .Select(src => StorageHelpers.FromPathAndType(
                            Path.Combine(Path.GetDirectoryName(src.rSrc.Source), Path.GetFileName(src.rSrc.Source).Replace("$R", "$I", StringComparison.Ordinal)),
                            src.oSrc.ItemType)).ToListAsync(), null, null, true, cancellationToken);
                    return new StorageHistory(FileOperationType.Restore, 
                        sourceMatch,
                        await movedSources.Zip(sourceMatch, (rSrc, oSrc) => new { rSrc, oSrc })
                            .Select(item => StorageHelpers.FromPathAndType(item.rSrc.Destination, item.oSrc.ItemType)).ToListAsync());
                }
                return null; // Cannot undo overwrite operation
            }
            else
            {
                // Retry failed operations
                var failedSources = moveResult.Items.Where(x => !x.Succeeded);
                var moveZip = source.Zip(destination, (src, dest) => new { src, dest });
                var sourceMatch = await failedSources.Select(x => moveZip.SingleOrDefault(s => s.src.Path == x.Source)).Where(x => x != null).ToListAsync();
                return await filesystemOperations.RestoreItemsFromTrashAsync(
                    await sourceMatch.Select(x => x.src).ToListAsync(),
                    await sourceMatch.Select(x => x.dest).ToListAsync(), progress, errorCode, cancellationToken);
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

        private struct HResult
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