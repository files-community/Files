using Files.Filesystem;
using Files.Filesystem.StorageItems;
using Files.Helpers;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace Files.DataModels
{
    public class ShellNewEntry
    {
        public string Extension { get; set; }
        public string Name { get; set; }
        public string Command { get; set; }
        public StorageItemThumbnail Icon { get; set; }
        public byte[] Data { get; set; }
        public string Template { get; set; }

        public async Task<FilesystemResult<BaseStorageFile>> Create(string filePath, IShellPage associatedInstance)
        {
            var parentFolder = await associatedInstance.FilesystemViewModel.GetFolderFromPathAsync(PathNormalization.GetParentDir(filePath));
            if (parentFolder)
            {
                return await Create(parentFolder, Path.GetFileName(filePath));
            }
            return new FilesystemResult<BaseStorageFile>(null, parentFolder.ErrorCode);
        }

        public async Task<FilesystemResult<BaseStorageFile>> Create(BaseStorageFolder parentFolder, string fileName)
        {
            FilesystemResult<BaseStorageFile> createdFile = null;
            if (!fileName.EndsWith(this.Extension))
            {
                fileName += this.Extension;
            }
            if (Template == null)
            {
                createdFile = await FilesystemTasks.Wrap(() => parentFolder.CreateFileAsync(fileName, CreationCollisionOption.GenerateUniqueName).AsTask());
            }
            else
            {
                createdFile = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFileFromPathAsync(Template))
                    .OnSuccess(t => t.CopyAsync(parentFolder, fileName, NameCollisionOption.GenerateUniqueName).AsTask());
            }
            if (createdFile)
            {
                if (this.Data != null)
                {
                    //await FileIO.WriteBytesAsync(createdFile.Result, this.Data); // Calls unsupported OpenTransactedWriteAsync
                    using (var fileStream = await createdFile.Result.OpenStreamForWriteAsync())
                    {
                        await fileStream.WriteAsync(Data, 0, Data.Length);
                        await fileStream.FlushAsync();
                    }
                }
            }
            return createdFile;
        }
    }
}