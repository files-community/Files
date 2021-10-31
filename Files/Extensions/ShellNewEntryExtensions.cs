using Files.Common;
using Files.Filesystem;
using Files.Filesystem.StorageItems;
using Files.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
using Windows.Storage;

namespace Files.Extensions
{
    public static class ShellNewEntryExtensions
    {
        public static async Task<List<ShellNewEntry>> GetNewContextMenuEntries()
        {
            var shellEntryList = new List<ShellNewEntry>();
            var connection = await AppServiceConnectionHelper.Instance;
            if (connection != null)
            {
                var (status, response) = await connection.SendMessageForResponseAsync(new ValueSet()
                {
                    { "Arguments", "GetNewContextMenuEntries" }
                });
                if (status == AppServiceResponseStatus.Success && response.ContainsKey("Entries"))
                {
                    var entries = JsonConvert.DeserializeObject<List<ShellNewEntry>>((string)response["Entries"]);
                    if (entries != null)
                    {
                        shellEntryList.AddRange(entries);
                    }
                }
            }
            return shellEntryList;
        }

        public static async Task<ShellNewEntry> GetNewContextMenuEntryForType(string extension)
        {
            var connection = await AppServiceConnectionHelper.Instance;
            if (connection != null)
            {
                var (status, response) = await connection.SendMessageForResponseAsync(new ValueSet()
                {
                    { "Arguments", "GetNewContextMenuEntryForType" },
                    { "extension", extension }
                });
                if (status == AppServiceResponseStatus.Success && response.ContainsKey("Entry"))
                {
                    return JsonConvert.DeserializeObject<ShellNewEntry>((string)response["Entry"]);
                }
            }
            return null;
        }

        public static async Task<FilesystemResult<BaseStorageFile>> Create(this ShellNewEntry shellEntry, string filePath, IShellPage associatedInstance)
        {
            var parentFolder = await associatedInstance.FilesystemViewModel.GetFolderFromPathAsync(PathNormalization.GetParentDir(filePath));
            if (parentFolder)
            {
                return await Create(shellEntry, parentFolder, Path.GetFileName(filePath));
            }
            return new FilesystemResult<BaseStorageFile>(null, parentFolder.ErrorCode);
        }

        public static async Task<FilesystemResult<BaseStorageFile>> Create(this ShellNewEntry shellEntry, BaseStorageFolder parentFolder, string fileName)
        {
            FilesystemResult<BaseStorageFile> createdFile = null;
            if (!fileName.EndsWith(shellEntry.Extension))
            {
                fileName += shellEntry.Extension;
            }
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
                    //await FileIO.WriteBytesAsync(createdFile.Result, this.Data); // Calls unsupported OpenTransactedWriteAsync
                    using (var fileStream = await createdFile.Result.OpenStreamForWriteAsync())
                    {
                        await fileStream.WriteAsync(shellEntry.Data, 0, shellEntry.Data.Length);
                        await fileStream.FlushAsync();
                    }
                }
            }
            return createdFile;
        }
    }
}