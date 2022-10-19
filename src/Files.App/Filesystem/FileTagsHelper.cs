using Common;
using Files.App.Filesystem.StorageItems;
using Files.App.Helpers;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;
using IO = System.IO;

namespace Files.App.Filesystem
{
    public static class FileTagsHelper
    {
        public static string FileTagsDbPath => IO.Path.Combine(ApplicationData.Current.LocalFolder.Path, "filetags.db");

        public static FileTagsDb DbInstance => new FileTagsDb(FileTagsDbPath, true);

        public static string[] ReadFileTag(string filePath)
        {
            var tagString = NativeFileOperationsHelper.ReadStringFromFile($"{filePath}:files");
            return tagString?.Split(',');
        }

        public static void WriteFileTag(string filePath, string[] tag)
        {
            var isDateOk = NativeFileOperationsHelper.GetFileDateModified(filePath, out var dateModified); // Backup date modified
            var isReadOnly = NativeFileOperationsHelper.HasFileAttribute(filePath, IO.FileAttributes.ReadOnly);
            if (isReadOnly) // Unset read-only attribute (#7534)
            {
                NativeFileOperationsHelper.UnsetFileAttribute(filePath, IO.FileAttributes.ReadOnly);
            }
            if (tag is null || !tag.Any())
            {
                NativeFileOperationsHelper.DeleteFileFromApp($"{filePath}:files");
            }
            else if (ReadFileTag(filePath) is not string[] arr || !tag.SequenceEqual(arr))
            {
                NativeFileOperationsHelper.WriteStringToFile($"{filePath}:files", string.Join(',', tag));
            }
            if (isReadOnly) // Restore read-only attribute (#7534)
            {
                NativeFileOperationsHelper.SetFileAttribute(filePath, IO.FileAttributes.ReadOnly);
            }
            if (isDateOk)
            {
                NativeFileOperationsHelper.SetFileDateModified(filePath, dateModified); // Restore date modified
            }
        }

        public static Task<ulong?> GetFileFRN(IStorageItem item)
        {
            return item switch
            {
                BaseStorageFolder { Properties: not null } folder => GetFileFRN(folder.Properties),
                BaseStorageFile { Properties: not null } file => GetFileFRN(file.Properties),
                _ => Task.FromResult<ulong?>(null),
            };

            static async Task<ulong?> GetFileFRN(IStorageItemExtraProperties properties)
            {
                var extra = await properties.RetrievePropertiesAsync(new string[] { "System.FileFRN" });
                return (ulong?)extra["System.FileFRN"];
            }
        }
    }
}