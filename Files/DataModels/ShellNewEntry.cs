using Files.Filesystem;
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

        public async Task<FilesystemResult<StorageFile>> Create(string filePath, IShellPage associatedInstance)
        {
            var parentFolder = await associatedInstance.FilesystemViewModel.GetFolderFromPathAsync(Path.GetDirectoryName(filePath));
            if (parentFolder)
            {
                return await Create(parentFolder, Path.GetFileName(filePath));
            }
            return new FilesystemResult<StorageFile>(null, parentFolder.ErrorCode);
        }

        public async Task<FilesystemResult<StorageFile>> Create(StorageFolder parentFolder, string fileName)
        {
            FilesystemResult<StorageFile> createdFile = null;
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
                createdFile = await FilesystemTasks.Wrap(() => StorageFile.GetFileFromPathAsync(Template).AsTask())
                    .OnSuccess(t => t.CopyAsync(parentFolder, fileName, NameCollisionOption.GenerateUniqueName).AsTask());
            }
            if (createdFile)
            {
                if (this.Data != null)
                {
                    await FileIO.WriteBytesAsync(createdFile.Result, this.Data);
                }
            }
            return createdFile;
        }
    }
}