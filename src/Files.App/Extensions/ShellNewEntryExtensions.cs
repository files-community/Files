using Files.Shared;
using Files.App.Filesystem;
using Files.App.Filesystem.StorageItems;
using Files.App.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Files.Shared.Extensions;
using Files.App.Shell;
using Files.App.ViewModels;

namespace Files.App.Extensions
{
    public static class ShellNewEntryExtensions
    {
        public static async Task<List<ShellNewEntry>> GetNewContextMenuEntries()
        {
            var shellEntryList = new List<ShellNewEntry>();
            var entries = await SafetyExtensions.IgnoreExceptions(() => ShellNewMenuHelper.GetNewContextMenuEntries(), App.Logger);
            if (entries != null)
            {
                shellEntryList.AddRange(entries);
            }
            return shellEntryList;
        }

        public static async Task<ShellNewEntry?> GetNewContextMenuEntryForType(string extension)
        {
            return await SafetyExtensions.IgnoreExceptions(() => ShellNewMenuHelper.GetNewContextMenuEntryForType(extension), App.Logger);
        }

        public static async Task<FilesystemResult<BaseStorageFile>> Create(this ShellNewEntry shellEntry, string filePath, LayoutModeViewModel associatedInstance)
        {
            var parentFolder = await associatedInstance.FilesystemViewModel.GetFolderFromPathAsync(PathNormalization.GetParentDir(filePath));
            if (parentFolder)
            {
                return await Create(shellEntry, parentFolder, filePath);
            }
            return new FilesystemResult<BaseStorageFile>(null, parentFolder.ErrorCode);
        }

        public static async Task<FilesystemResult<BaseStorageFile>> Create(this ShellNewEntry shellEntry, BaseStorageFolder parentFolder, string filePath)
        {
            FilesystemResult<BaseStorageFile> createdFile = null;
            var fileName = Path.GetFileName(filePath);
            if (shellEntry.Template == null)
            {
                createdFile = await FilesystemTasks.Wrap(() => parentFolder.CreateFileAsync(fileName, CreationCollisionOption.GenerateUniqueName).AsTask());
            }
            else
            {
                createdFile = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFileFromPathAsync(shellEntry.Template))
                    .OnSuccess(t => t.CopyAsync(parentFolder, fileName, NameCollisionOption.GenerateUniqueName).AsTask());
            }
            if (createdFile)
            {
                if (shellEntry.Data != null)
                {
                    //await FileIO.WriteBytesAsync(createdFile.Result, shellEntry.Data); // Calls unsupported OpenTransactedWriteAsync
                    await createdFile.Result.WriteBytesAsync(shellEntry.Data);
                }
            }
            return createdFile;
        }
    }
}