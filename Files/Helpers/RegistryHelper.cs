using Files.Filesystem;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace Files.Helpers
{
    public class RegistryHelper
    {
        public static async Task<List<ShellNewEntry>> GetNewContextMenuEntries()
        {
            var newMenuItems = new List<ShellNewEntry>();
            foreach (var keyName in Registry.ClassesRoot.GetSubKeyNames()
                .Where(x => x.StartsWith(".") && !new string[] { ".library-ms" }.Contains(x)))
            {
                using var key = Registry.ClassesRoot.OpenSubKey(keyName);
                var ret = await GetShellNewRegistryEntries(key, key);
                if (ret != null)
                {
                    newMenuItems.Add(ret);
                }
            }
            return newMenuItems;
        }

        public static async Task<ShellNewEntry> GetNewContextMenuEntryForType(string extension)
        {
            using var key = Registry.ClassesRoot.OpenSubKey(extension);
            return key != null ? await GetShellNewRegistryEntries(key, key) : null;
        }

        private static async Task<ShellNewEntry> GetShellNewRegistryEntries(RegistryKey current, RegistryKey root)
        {
            foreach (var keyName in current.GetSubKeyNames())
            {
                using var key = current.OpenSubKey(keyName);
                if (keyName == "ShellNew")
                {
                    return await ParseShellNewRegistryEntry(key, root);
                }
                else
                {
                    var ret = await GetShellNewRegistryEntries(key, root);
                    if (ret != null)
                    {
                        return ret;
                    }
                }
            }
            return null;
        }

        private static async Task<ShellNewEntry> ParseShellNewRegistryEntry(RegistryKey key, RegistryKey root)
        {
            if (!key.GetValueNames().Contains("NullFile") && 
                !key.GetValueNames().Contains("ItemName") &&
                !key.GetValueNames().Contains("FileName"))
            {
                return null;
            }
            
            var extension = root.Name.Substring(root.Name.LastIndexOf('\\') + 1);
            var fileName = (string)key.GetValue("FileName");
            if (!string.IsNullOrEmpty(fileName) && Path.GetExtension(fileName) != extension)
            {
                return null;
            }

            byte[] data = null;
            var dataObj = key.GetValue("Data");
            if (dataObj != null)
            {
                switch (key.GetValueKind("Data"))
                {
                    case RegistryValueKind.Binary:
                        data = (byte[])dataObj;
                        break;
                    case RegistryValueKind.String:
                        data = UTF8Encoding.UTF8.GetBytes((string)dataObj);
                        break;
                }
            }

            var entry = new ShellNewEntry() { 
                Extension = extension,
                Template = fileName,
                //Name = (string)key.GetValue("ItemName"),
                Name = await GetExtensionDisplayType(extension),
                Command = (string)key.GetValue("Command"),
                IconPath = (string)key.GetValue("IconPath"),
                Data = data
            };

    return entry;
        }

        private static async Task<string> GetExtensionDisplayType(string extension)
        {
            var displayType = await FilesystemTasks.Wrap(() => ApplicationData.Current.LocalFolder.CreateFolderAsync("extensions", CreationCollisionOption.OpenIfExists).AsTask())
                .OnSuccess(t => t.CreateFileAsync("file" + extension, CreationCollisionOption.OpenIfExists).AsTask())
                .OnSuccess(f => Task.FromResult(f.DisplayType));

            return displayType ?? string.Format("{0} {1}", "file", extension);
        }

        public class ShellNewEntry
        {
            public string Extension { get; set; }
            public string Name { get; set; }
            public string Command { get; set; }
            public string IconPath { get; set; }
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
                    createdFile = await FilesystemTasks.Wrap(() => parentFolder.CreateFileAsync(fileName).AsTask());
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
}
