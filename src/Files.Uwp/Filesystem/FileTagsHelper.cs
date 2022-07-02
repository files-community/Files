using Common;
using Files.Uwp.Filesystem.StorageItems;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;
using static Files.Uwp.Filesystem.Native.NativeApi;
using static Files.Uwp.Filesystem.Native.NativeHelpers;
using IO = System.IO;

namespace Files.Uwp.Filesystem
{
    public static class FileTagsHelper
    {
        public static string FileTagsDbPath => IO.Path.Combine(ApplicationData.Current.LocalFolder.Path, "filetags.db");

        private static readonly Lazy<FileTagsDb> dbInstance = new(() => new FileTagsDb(FileTagsDbPath, true));
        public static FileTagsDb DbInstance => dbInstance.Value;

        public static string[] ReadFileTag(string filePath)
        {
            var tagString = ReadStringFromFile($"{filePath}:files");
            return tagString?.Split(',');
        }

        public static void WriteFileTag(string filePath, string[] tag)
        {
            var isDateOk = GetFileDateModified(filePath, out var dateModified); // Backup date modified
            var isReadOnly = HasFileAttribute(filePath, IO.FileAttributes.ReadOnly);
            if (isReadOnly) // Unset read-only attribute (#7534)
            {
                UnsetFileAttribute(filePath, IO.FileAttributes.ReadOnly);
            }
            if (tag is null || !tag.Any())
            {
                DeleteFileFromApp($"{filePath}:files");
            }
            else if (ReadFileTag(filePath) is not string[] arr || !tag.SequenceEqual(arr))
            {
                WriteStringToFile($"{filePath}:files", string.Join(',', tag));
            }
            if (isReadOnly) // Restore read-only attribute (#7534)
            {
                SetFileAttribute(filePath, IO.FileAttributes.ReadOnly);
            }
            if (isDateOk)
            {
                SetFileDateModified(filePath, dateModified); // Restore date modified
            }
        }

        public static async Task<ulong?> GetFileFRN(IStorageItem item)
        {
            return item switch
            {
                BaseStorageFolder { Properties: not null } folder => await GetFileFRN(folder.Properties),
                BaseStorageFile { Properties: not null } file => await GetFileFRN(file.Properties),
                _ => null,
            };

            static async Task<ulong?> GetFileFRN(IStorageItemExtraProperties properties)
            {
                var extra = await properties.RetrievePropertiesAsync(new string[] { "System.FileFRN" });
                return (ulong?)extra["System.FileFRN"];
            }
        }
    }
}