using Common;
using Files.Filesystem.StorageItems;
using Files.Helpers;
using Files.Models.Settings;
using Microsoft.Toolkit.Uwp;
using Microsoft.Toolkit.Uwp.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
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
            if (tag == null)
            {
                NativeFileOperationsHelper.DeleteFileFromApp($"{filePath}:files");
            }
            else if (ReadFileTag(filePath) != tag)
            {
                NativeFileOperationsHelper.WriteStringToFile($"{filePath}:files", tag);
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

    public class FileTag
    {
        public string TagName { get; set; }
        public string Uid { get; set; }
        public string ColorString { get; set; }

        private SolidColorBrush color;

        [JsonIgnore]
        public SolidColorBrush Color => color ??= new SolidColorBrush(ColorString.ToColor());

        public FileTag(string tagName, string colorString)
        {
            TagName = tagName;
            ColorString = colorString;
            Uid = Guid.NewGuid().ToString();
        }

        [JsonConstructor]
        public FileTag(string tagName, string colorString, string uid)
        {
            TagName = tagName;
            ColorString = colorString;
            Uid = uid;
        }
    }

    public class FileTagsSettings : BaseJsonSettingsModel
    {
        public FileTagsSettings()
            : base(System.IO.Path.Combine(ApplicationData.Current.LocalFolder.Path, Constants.LocalSettings.SettingsFolderName, Constants.LocalSettings.FileTagSettingsFileName),
                  isCachingEnabled: true)
        {
        }

        public IList<FileTag> FileTagList
        {
            get => Get<IList<FileTag>>(() => new List<FileTag>()
            {
                new FileTag("Blue", "#0072BD"),
                new FileTag("Orange", "#D95319"),
                new FileTag("Yellow", "#EDB120"),
                new FileTag("Green", "#77AC30"),
                new FileTag("Azure", "#4DBEEE")
            });
            set => Set(value);
        }

        public FileTag GetTagByID(string uid)
        {
            var tag = FileTagList.SingleOrDefault(x => x.Uid == uid);
            if (!string.IsNullOrEmpty(uid) && tag == null)
            {
                tag = new FileTag("FileTagUnknown".GetLocalized(), "#9ea3a1", uid);
                FileTagList = FileTagList.Append(tag).ToList();
            }
            return tag;
        }

        public IEnumerable<FileTag> GetTagsByName(string tagName)
        {
            return FileTagList.Where(x => x.TagName.Equals(tagName, StringComparison.OrdinalIgnoreCase));
        }
    }
}