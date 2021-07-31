using Common;
using Files.Helpers;
using Microsoft.Toolkit.Uwp.Helpers;
using Newtonsoft.Json;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml.Media;

namespace Files.Filesystem
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
                    _DbInstance = new FileTagsDb(FileTagsDbPath);
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
            if (tag == null)
            {
                NativeFileOperationsHelper.DeleteFileFromApp($"{filePath}:files");
            }
            else
            {
                NativeFileOperationsHelper.WriteStringToFile($"{filePath}:files", tag);
            }
        }

        public static async Task<ulong?> GetFileFRN(IStorageItem item)
        {
            if (item is StorageFolder folderItem)
            {
                var extraProperties = await folderItem.Properties.RetrievePropertiesAsync(new string[] { "System.FileFRN" });
                return (ulong?)extraProperties["System.FileFRN"];
            }
            else if (item is StorageFile fileItem)
            {
                var extraProperties = await fileItem.Properties.RetrievePropertiesAsync(new string[] { "System.FileFRN" });
                return (ulong?)extraProperties["System.FileFRN"];
            }
            return null;
        }

        public static ulong? GetFileFRN(string filePath)
        {
            var hFile = NativeFileOperationsHelper.CreateFileFromApp(filePath, NativeFileOperationsHelper.GENERIC_READ, 0, IntPtr.Zero, NativeFileOperationsHelper.OPEN_EXISTING, (uint)NativeFileOperationsHelper.File_Attributes.BackupSemantics, IntPtr.Zero);
            if (hFile.ToInt64() != -1)
            {
                var res = new NativeFileOperationsHelper.FILE_ID_INFO();
                if (NativeFileOperationsHelper.GetFileInformationByHandleEx(hFile, NativeFileOperationsHelper.FILE_INFO_BY_HANDLE_CLASS.FileIdInfo, ref res, (uint)Marshal.SizeOf(res)))
                {
                    return BitConverter.ToUInt64(res.FileId.Identifier, 0);
                }
            }
            return null;
        }

        private FileTagsHelper()
        {
        }
    }

    public class FileTag
    {
        public string TagName { get; set; }
        public string Uid { get; set; }
        public string ColorString { get; set; }
        
        [JsonIgnore]
        public SolidColorBrush Color { get; set; }

        public FileTag(string tagName, string colorString)
        {
            TagName = tagName;
            ColorString = colorString;
            Color = new SolidColorBrush(colorString.ToColor());
            Uid = Guid.NewGuid().ToString();
        }
    }
}
