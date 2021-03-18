using Files.Enums;
using Files.Filesystem.FilesystemHistory;
using Files.Filesystem.StorageFileHelpers;
using FluentFTP;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace Files.Filesystem
{
    public class FtpFilesystemOperaions : IFilesystemOperations
    {
        public async Task<IStorageHistory> CopyAsync(IStorageItem source, string destination, IProgress<float> progress, IProgress<FileSystemStatusCode> errorCode, CancellationToken cancellationToken)
        {
            await Task.Run(async () =>
            {
                if (!(source is StorageFile item))
                {
                    return;
                }

                var ftpProgress = new Progress<FtpProgress>();

                errorCode.Report(FileSystemStatusCode.InProgress);

                ftpProgress.ProgressChanged += (_, args) =>
                {
                    progress.Report(Convert.ToSingle(args.Progress));
                };

                try
                {

                    if (item.IsOfType(StorageItemTypes.File))
                    {
                        using var fileStream = new FileStream(destination, FileMode.Create);
                        await item.CopyAsync(await StorageFolder.GetFolderFromPathAsync(destination));
                        errorCode.Report(FileSystemStatusCode.Success);
                        return;
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    errorCode.Report(FileSystemStatusCode.Unauthorized);
                }
                catch
                {
                    errorCode.Report(FileSystemStatusCode.Generic);
                }
            });

            return null;
        }

        public Task<IStorageHistory> CopyAsync(IStorageItemWithPath source, string destination, IProgress<float> progress, IProgress<FileSystemStatusCode> errorCode, CancellationToken cancellationToken)
        {
            return CopyAsync(source.Item, destination, progress, errorCode, cancellationToken);
        }

        public Task<IStorageHistory> CreateAsync(IStorageItemWithPath source, IProgress<FileSystemStatusCode> errorCode, CancellationToken cancellationToken) => throw new NotImplementedException();
        public async Task<IStorageHistory> DeleteAsync(IStorageItem source, IProgress<float> progress, IProgress<FileSystemStatusCode> errorCode, bool permanently, CancellationToken cancellationToken)
        {
            await Task.Run(async () =>
            {
                errorCode.Report(FileSystemStatusCode.InProgress);
                progress.Report(0);
                try
                {
                    await source.DeleteAsync();
                    progress.Report(100);
                    errorCode.Report(FileSystemStatusCode.Success);
                }
                catch
                {
                    errorCode.Report(FileSystemStatusCode.Generic);
                }
            });

            return null;
        }
        public Task<IStorageHistory> DeleteAsync(IStorageItemWithPath source, IProgress<float> progress, IProgress<FileSystemStatusCode> errorCode, bool permanently, CancellationToken cancellationToken)
        {
            return DeleteAsync(source.Item, progress, errorCode, permanently, cancellationToken);
        }

        public void Dispose() { }

        public async Task<IStorageHistory> MoveAsync(IStorageItem source, string destination, IProgress<float> progress, IProgress<FileSystemStatusCode> errorCode, CancellationToken cancellationToken)
        {
            await CopyAsync(source, destination, progress, errorCode, cancellationToken);
            await DeleteAsync(source, progress, errorCode, true, cancellationToken);

            return null;
        }
        public Task<IStorageHistory> MoveAsync(IStorageItemWithPath source, string destination, IProgress<float> progress, IProgress<FileSystemStatusCode> errorCode, CancellationToken cancellationToken)
        {
            return MoveAsync(source.Item, destination, progress, errorCode, cancellationToken);
        }
        public async Task<IStorageHistory> RenameAsync(IStorageItem source, string newName, NameCollisionOption collision, IProgress<FileSystemStatusCode> errorCode, CancellationToken cancellationToken)
        {
            await Task.Run(async () =>
            {
                errorCode.Report(FileSystemStatusCode.InProgress); ;
                try
                {
                    await source.RenameAsync(newName, collision);
                    errorCode.Report(FileSystemStatusCode.Success);
                }
                catch
                {
                    errorCode.Report(FileSystemStatusCode.Generic);
                }
            });

            return null;
        }

        public Task<IStorageHistory> RenameAsync(IStorageItemWithPath source, string newName, NameCollisionOption collision, IProgress<FileSystemStatusCode> errorCode, CancellationToken cancellationToken)
        {
            return RenameAsync(source.Item, newName, collision, errorCode, cancellationToken);
        }

        public Task<IStorageHistory> RestoreFromTrashAsync(IStorageItemWithPath source, string destination, IProgress<float> progress, IProgress<FileSystemStatusCode> errorCode, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }
}
