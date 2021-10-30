using Files.Common;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace FilesFullTrust.Helpers
{
    public static class ShellNewMenuHelper
    {
        public static async Task<List<ShellNewEntry>> GetNewContextMenuEntries()
        {
            var newMenuItems = new List<ShellNewEntry>();
            foreach (var keyName in Registry.ClassesRoot.GetSubKeyNames().Where(x => x.StartsWith(".") && !new string[] { ShellLibraryItem.EXTENSION, ".url", ".lnk" }.Contains(x)))
            {
                using var key = Registry.ClassesRoot.OpenSubKeySafe(keyName);
                if (key != null)
                {
                    var ret = await GetShellNewRegistryEntries(key, key);
                    if (ret != null)
                    {
                        newMenuItems.Add(ret);
                    }
                }
            }
            return newMenuItems;
        }

        public static async Task<ShellNewEntry> GetNewContextMenuEntryForType(string extension)
        {
            if (string.IsNullOrEmpty(extension)) return null;
            using var key = Registry.ClassesRoot.OpenSubKeySafe(extension);
            return key != null ? await GetShellNewRegistryEntries(key, key) : null;
        }

        private static async Task<ShellNewEntry> GetShellNewRegistryEntries(RegistryKey current, RegistryKey root)
        {
            foreach (var keyName in current.GetSubKeyNames())
            {
                using var key = current.OpenSubKeySafe(keyName);
                if (key == null)
                {
                    continue;
                }
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

            var folder = await Extensions.IgnoreExceptions(() => ApplicationData.Current.LocalFolder.CreateFolderAsync("extensions", CreationCollisionOption.OpenIfExists).AsTask());
            var sampleFile = folder != null ? await Extensions.IgnoreExceptions(() => folder.CreateFileAsync("file" + extension, CreationCollisionOption.OpenIfExists).AsTask()) : null;

            var displayType = sampleFile != null ? sampleFile.DisplayType : string.Format("{0} {1}", "file", extension);
            var thumbnail = sampleFile != null ? await Extensions.IgnoreExceptions(() => sampleFile.GetThumbnailAsync(Windows.Storage.FileProperties.ThumbnailMode.ListView, 24, Windows.Storage.FileProperties.ThumbnailOptions.UseCurrentScale).AsTask()) : null;

            string iconString = null;
            if (thumbnail != null)
            {
                var readStream = thumbnail.AsStreamForRead();
                var bitmapData = new byte[readStream.Length];
                await readStream.ReadAsync(bitmapData, 0, bitmapData.Length);
                iconString = Convert.ToBase64String(bitmapData, 0, bitmapData.Length);
            }

            var entry = new ShellNewEntry()
            {
                Extension = extension,
                Template = fileName,
                Name = displayType,
                Command = (string)key.GetValue("Command"),
                IconBase64 = iconString,
                Data = data
            };

            return entry;
        }

        private static RegistryKey OpenSubKeySafe(this RegistryKey root, string keyName)
        {
            try
            {
                return root.OpenSubKey(keyName);
            }
            catch (SecurityException)
            {
                return null;
            }
        }
    }
}