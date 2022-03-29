using Files.Shared.Extensions;
using Files.Shared.Enums;
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
using Files.Shared;
using Files.Backend.ViewModels.Dialogs;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.Backend.Services;

namespace Files.Filesystem
{
    public class ShellFilesystemOperations : IFilesystemOperations
    {
        #region Private Members

        private IShellPage associatedInstance;

        private FilesystemOperations filesystemOperations;

        private RecycleBinHelpers recycleBinHelpers;

        private IDialogService DialogService { get; } = Ioc.Default.GetRequiredService<IDialogService>();

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
                if (copyResult.Items.Any(x => HResult.Convert(x.HResult) == FileSystemStatusCode.Unauthorized))
                {
                    if (await RequestAdminOperation())
                    {
                        return await CopyItemsAsync(source, destination, collisions, progress, errorCode, cancellationToken);
                    }
                }
                errorCode?.Report(HResult.Convert(copyResult.Items.FirstOrDefault(x => !x.Succeeded)?.HResult));
                return null;
            }
        }

        public async Task<(IStorageHistory, IStorageItem)> CreateAsync(IStorageItemWithPath source, IProgress<FileSystemStatusCode> errorCode, CancellationToken cancellationToken)
        {
            var connection = await AppServiceConnectionHelper.Instance;
            if (connection == null || string.IsNullOrWhiteSpace(source.Path) || source.Path.StartsWith(@"\\?\", StringComparison.Ordinal) || FtpHelpers.IsFtpPath(source.Path))
            {
                // Fallback to builtin file operations
                return await filesystemOperations.CreateAsync(source, errorCode, cancellationToken);
            }

            var createResult = new ShellOperationResult();
            (var status, var response) = (AppServiceResponseStatus.Failure, (Dictionary<string, object>)null);

            switch (source.ItemType)
            {
                case FilesystemItemType.File:
                    {
                        var newEntryInfo = await ShellNewEntryExtensions.GetNewContextMenuEntryForType(Path.GetExtension(source.Path));
                        if (newEntryInfo?.Command != null)
                        {
                            var args = CommandLine.CommandLineParser.SplitArguments(newEntryInfo.Command);
                            if (args.Any())
                            {
                                (status, response) = await connection.SendMessageForResponseAsync(new ValueSet()
                                {
                                    { "Arguments", "LaunchApp" },
                                    { "WorkingDirectory", PathNormalization.GetParentDir(source.Path) },
                                    { "Application", args[0].Replace("\"", "", StringComparison.Ordinal) },
                                    { "Parameters", string.Join(" ", args.Skip(1)).Replace("%1", source.Path) }
                                });
                            }
                        }
                        else
                        {
                            (status, response) = await connection.SendMessageForResponseAsync(new ValueSet()
                            {
                                { "Arguments", "FileOperation" },
                                { "fileop", "CreateFile" },
                                { "filepath", source.Path },
                                { "template", newEntryInfo?.Template },
                                { "data", newEntryInfo?.Data }
                            });
                        }
                        break;
                    }
                case FilesystemItemType.Directory:
                    {
                        (status, response) = await connection.SendMessageForResponseAsync(new ValueSet()
                        {
                            { "Arguments", "FileOperation" },
                            { "fileop", "CreateFolder" },
                            { "filepath", source.Path }
                        });
                        break;
                    }
            }

            var result = (FilesystemResult)(status == AppServiceResponseStatus.Success
                && response.Get("Success", false));
            var shellOpResult = JsonConvert.DeserializeObject<ShellOperationResult>(response.Get("Result", ""));
            createResult.Items.AddRange(shellOpResult?.Items ?? Enumerable.Empty<ShellOperationItemResult>());

            result &= (FilesystemResult)createResult.Items.All(x => x.Succeeded);

            if (result)
            {
                errorCode?.Report(FileSystemStatusCode.Success);
                var createdSources = createResult.Items.Where(x => x.Succeeded && x.Destination != null && x.Source != x.Destination);
                if (createdSources.Any())
                {
                    var item = StorageHelpers.FromPathAndType(createdSources.Single().Destination, source.ItemType);
                    return (new StorageHistory(FileOperationType.CreateNew, item.CreateList(), null), item.Item);
                }
                return (null, null);
            }
            else
            {
                if (createResult.Items.Any(x => HResult.Convert(x.HResult) == FileSystemStatusCode.Unauthorized))
                {
                    if (await RequestAdminOperation())
                    {
                        return await CreateAsync(source, errorCode, cancellationToken);
                    }
                }
                errorCode?.Report(HResult.Convert(createResult.Items.FirstOrDefault(x => !x.Succeeded)?.HResult));
                return (null, null);
            }
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
                if (deleteResult.Items.Any(x => HResult.Convert(x.HResult) == FileSystemStatusCode.Unauthorized))
                {
                    if (await RequestAdminOperation())
                    {
                        return await DeleteItemsAsync(source, progress, errorCode, permanently, cancellationToken);
                    }
                }
                errorCode?.Report(HResult.Convert(deleteResult.Items.FirstOrDefault(x => !x.Succeeded)?.HResult));
                return null;
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
                if (moveResult.Items.Any(x => HResult.Convert(x.HResult) == FileSystemStatusCode.Unauthorized))
                {
                    if (await RequestAdminOperation())
                    {
                        return await MoveItemsAsync(source, destination, collisions, progress, errorCode, cancellationToken);
                    }
                }
                errorCode?.Report(HResult.Convert(moveResult.Items.FirstOrDefault(x => !x.Succeeded)?.HResult));
                return null;
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
                if (renameResult.Items.Any(x => HResult.Convert(x.HResult) == FileSystemStatusCode.Unauthorized))
                {
                    if (await RequestAdminOperation())
                    {
                        return await RenameAsync(source, newName, collision, errorCode, cancellationToken);
                    }
                }
                errorCode?.Report(HResult.Convert(renameResult.Items.FirstOrDefault(x => !x.Succeeded)?.HResult));
                return null;
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
                if (moveResult.Items.Any(x => HResult.Convert(x.HResult) == FileSystemStatusCode.Unauthorized))
                {
                    if (await RequestAdminOperation())
                    {
                        return await RestoreItemsFromTrashAsync(source, destination, progress, errorCode, cancellationToken);
                    }
                }
                errorCode?.Report(HResult.Convert(moveResult.Items.FirstOrDefault(x => !x.Succeeded)?.HResult));
                return null;
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

        private async Task<bool> RequestAdminOperation()
        {
            if (!App.MainViewModel.IsFullTrustElevated)
            {
                if (await DialogService.ShowDialogAsync(new ElevateConfirmDialogViewModel()) == DialogResult.Primary)
                {
                    var connection = await AppServiceConnectionHelper.Instance;
                    if (connection != null && await connection.Elevate())
                    {
                        connection = await AppServiceConnectionHelper.Instance;
                        return connection != null;
                    }
                }
            }
            return false;
        }

        private struct HResult
        {
            // https://github.com/RickStrahl/DeleteFiles/blob/master/DeleteFiles/ZetaLongPaths/Native/FileOperations/Interop/CopyEngineResult.cs
            // Ok
            public const int S_OK = 0;
            public const int COPYENGINE_S_DONT_PROCESS_CHILDREN = 2555912;
            public const int COPYENGINE_E_USER_CANCELLED = -2144927744;
            // Access denied
            public const int COPYENGINE_E_ACCESS_DENIED_SRC = -2144927711;
            public const int COPYENGINE_E_ACCESS_DENIED_DEST = -2144927710;
            public const int COPYENGINE_E_REQUIRES_ELEVATION = -2144927742;
            // Path too long
            public const int COPYENGINE_E_PATH_TOO_DEEP_SRC = -2144927715;
            public const int COPYENGINE_E_PATH_TOO_DEEP_DEST = -2144927714;
            public const int COPYENGINE_E_RECYCLE_PATH_TOO_LONG = -2144927688;
            public const int COPYENGINE_E_NEWFILE_NAME_TOO_LONG = -2144927685;
            public const int COPYENGINE_E_NEWFOLDER_NAME_TOO_LONG = -2144927684;
            // Not found
            public const int COPYENGINE_E_RECYCLE_BIN_NOT_FOUND = -2144927686;
            public const int COPYENGINE_E_PATH_NOT_FOUND_SRC = -2144927709;
            public const int COPYENGINE_E_PATH_NOT_FOUND_DEST = -2144927708;
            public const int COPYENGINE_E_NET_DISCONNECT_DEST = -2144927706;
            public const int COPYENGINE_E_NET_DISCONNECT_SRC = -2144927707;
            public const int COPYENGINE_E_CANT_REACH_SOURCE = -2144927691;
            // File in use
            public const int COPYENGINE_E_SHARING_VIOLATION_SRC = -2144927705;
            public const int COPYENGINE_E_SHARING_VIOLATION_DEST = -2144927704;
            // Already exists
            public const int COPYENGINE_E_ALREADY_EXISTS_NORMAL = -2144927703;
            public const int COPYENGINE_E_ALREADY_EXISTS_READONLY = -2144927702;
            public const int COPYENGINE_E_ALREADY_EXISTS_SYSTEM = -2144927701;
            public const int COPYENGINE_E_ALREADY_EXISTS_FOLDER = -2144927700;
            // File too big
            //public const int COPYENGINE_E_FILE_TOO_LARGE = -2144927731;
            //public const int COPYENGINE_E_REMOVABLE_FULL = -2144927730;
            //public const int COPYENGINE_E_DISK_FULL = -2144927694;
            //public const int COPYENGINE_E_DISK_FULL_CLEAN = -2144927693;
            //public const int COPYENGINE_E_RECYCLE_SIZE_TOO_BIG = -2144927689;
            // Invalid path
            public const int COPYENGINE_E_FILE_IS_FLD_DEST = -2144927732;
            public const int COPYENGINE_E_FLD_IS_FILE_DEST = -2144927733;
            //public const int COPYENGINE_E_INVALID_FILES_SRC = -2144927717;
            //public const int COPYENGINE_E_INVALID_FILES_DEST = -2144927716;
            //public const int COPYENGINE_E_SAME_FILE = -2144927741;
            //public const int COPYENGINE_E_DEST_SAME_TREE = -2144927734;
            //public const int COPYENGINE_E_DEST_SUBTREE = -2144927735;
            //public const int COPYENGINE_E_DIFF_DIR = -2144927740;

            public static FileSystemStatusCode Convert(int? hres)
            {
                return hres switch
                {
                    HResult.S_OK => FileSystemStatusCode.Success,
                    HResult.COPYENGINE_E_ACCESS_DENIED_SRC => FileSystemStatusCode.Unauthorized,
                    HResult.COPYENGINE_E_ACCESS_DENIED_DEST => FileSystemStatusCode.Unauthorized,
                    HResult.COPYENGINE_E_REQUIRES_ELEVATION => FileSystemStatusCode.Unauthorized,
                    HResult.COPYENGINE_E_RECYCLE_PATH_TOO_LONG => FileSystemStatusCode.NameTooLong,
                    HResult.COPYENGINE_E_NEWFILE_NAME_TOO_LONG => FileSystemStatusCode.NameTooLong,
                    HResult.COPYENGINE_E_NEWFOLDER_NAME_TOO_LONG => FileSystemStatusCode.NameTooLong,
                    HResult.COPYENGINE_E_PATH_TOO_DEEP_SRC => FileSystemStatusCode.NameTooLong,
                    HResult.COPYENGINE_E_PATH_TOO_DEEP_DEST => FileSystemStatusCode.NameTooLong,
                    HResult.COPYENGINE_E_PATH_NOT_FOUND_SRC => FileSystemStatusCode.NotFound,
                    HResult.COPYENGINE_E_PATH_NOT_FOUND_DEST => FileSystemStatusCode.NotFound,
                    HResult.COPYENGINE_E_NET_DISCONNECT_DEST => FileSystemStatusCode.NotFound,
                    HResult.COPYENGINE_E_NET_DISCONNECT_SRC => FileSystemStatusCode.NotFound,
                    HResult.COPYENGINE_E_CANT_REACH_SOURCE => FileSystemStatusCode.NotFound,
                    HResult.COPYENGINE_E_RECYCLE_BIN_NOT_FOUND => FileSystemStatusCode.NotFound,
                    HResult.COPYENGINE_E_ALREADY_EXISTS_NORMAL => FileSystemStatusCode.AlreadyExists,
                    HResult.COPYENGINE_E_ALREADY_EXISTS_READONLY => FileSystemStatusCode.AlreadyExists,
                    HResult.COPYENGINE_E_ALREADY_EXISTS_SYSTEM => FileSystemStatusCode.AlreadyExists,
                    HResult.COPYENGINE_E_ALREADY_EXISTS_FOLDER => FileSystemStatusCode.AlreadyExists,
                    HResult.COPYENGINE_E_FILE_IS_FLD_DEST => FileSystemStatusCode.NotAFile,
                    HResult.COPYENGINE_E_FLD_IS_FILE_DEST => FileSystemStatusCode.NotAFolder,
                    HResult.COPYENGINE_E_SHARING_VIOLATION_SRC => FileSystemStatusCode.InUse,
                    HResult.COPYENGINE_E_SHARING_VIOLATION_DEST => FileSystemStatusCode.InUse,
                    null => FileSystemStatusCode.Generic,
                    _ => FileSystemStatusCode.Generic
                };
            }
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