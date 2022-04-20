using Common;
using Files.Uwp.Filesystem.StorageItems;
using Files.Uwp.Helpers;
using System;
using System.Threading.Tasks;
using Windows.Storage;

namespace Files.Uwp.Filesystem
{
    public class FileTagsHelper
    {
        public static string FileTagsDbPath => System.IO.Path.Combine(ApplicationData.Current.LocalFolder.Path, "filetags.db");

        private static FileTagsDb _DbInstance;

        public static FileTagsDb DbInstance
        {
            get
            {
                if (_DbInstance == null)
                {
                    _DbInstance = new FileTagsDb(FileTagsDbPath, true);
                }
                return _DbInstance;
            }
        }

        public static string ReadFileTag(string filePath)
        {
            return NativeFileOperationsHelper.ReadStringFromFile($"{filePath}:files");
        }

        public static void WriteFileTag(string filePath, string tag)
        {
            var dateOk = NativeFileOperationsHelper.GetFileDateModified(filePath, out var dateModified); // Backup date modified
            var isReadOnly = NativeFileOperationsHelper.HasFileAttribute(filePath, System.IO.FileAttributes.ReadOnly);
            if (isReadOnly) // Unset read-only attribute (#7534)
            {
                NativeFileOperationsHelper.UnsetFileAttribute(filePath, System.IO.FileAttributes.ReadOnly);
            }
            if (tag == null)
            {
                NativeFileOperationsHelper.DeleteFileFromApp($"{filePath}:files");
            }
            else if (ReadFileTag(filePath) != tag)
            {
                NativeFileOperationsHelper.WriteStringToFile($"{filePath}:files", tag);
            }
            if (isReadOnly) // Restore read-only attribute (#7534)
            {
                NativeFileOperationsHelper.SetFileAttribute(filePath, System.IO.FileAttributes.ReadOnly);
            }
            if (dateOk)
            {
                NativeFileOperationsHelper.SetFileDateModified(filePath, dateModified); // Restore date modified
            }
        }

        public static async Task<ulong?> GetFileFRN(IStorageItem item)
        {
            if (item is BaseStorageFolder folderItem && folderItem.Properties != null)
            {
                var extraProperties = await folderItem.Properties.RetrievePropertiesAsync(new string[] { "System.FileFRN" });
                return (ulong?)extraProperties["System.FileFRN"];
            }
            else if (item is BaseStorageFile fileItem && fileItem.Properties != null)
            {
                var extraProperties = await fileItem.Properties.RetrievePropertiesAsync(new string[] { "System.FileFRN" });
                return (ulong?)extraProperties["System.FileFRN"];
            }
            return null;
        }

        private FileTagsHelper()
        {
        }
    }
}