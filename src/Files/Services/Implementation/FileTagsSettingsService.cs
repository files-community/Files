using Files.Backend.Models;
using Files.Models;
using Files.Backend.Models.JsonSettings;
using Files.Backend.Services.Settings;
using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Storage;

namespace Files.Services.Implementation
{
    public sealed class FileTagsSettingsService : BaseJsonSettingsModel, IFileTagsSettingsService
    {
        public event EventHandler OnSettingImportedEvent;

        private static readonly List<IFileTag> s_defaultFileTags = new List<IFileTag>()
        {
            new FileTagModel("Blue", "#0072BD"),
            new FileTagModel("Orange", "#D95319"),
            new FileTagModel("Yellow", "#EDB120"),
            new FileTagModel("Green", "#77AC30"),
            new FileTagModel("Azure", "#4DBEEE")
        };

        public FileTagsSettingsService()
            : base(System.IO.Path.Combine(ApplicationData.Current.LocalFolder.Path, Constants.LocalSettings.SettingsFolderName, Constants.LocalSettings.FileTagSettingsFileName),
                  isCachingEnabled: true)
        {
        }

        public IList<IFileTag> FileTagList
        {
            get => Get<List<IFileTag>>(s_defaultFileTags);
            set => Set(value);
        }

        public IFileTag GetTagById(string uid)
        {
            if (FileTagList.Any(x => x.Uid == null))
            {
                App.Logger.Warn("Tags file is invalid, regenerate");
                FileTagList = s_defaultFileTags;
            }
            var tag = FileTagList.SingleOrDefault(x => x.Uid == uid);
            if (!string.IsNullOrEmpty(uid) && tag == null)
            {
                tag = new FileTagModel("FileTagUnknown".GetLocalized(), "#9ea3a1", uid);
                FileTagList = FileTagList.Append(tag).ToList();
            }
            return tag;
        }

        public IEnumerable<IFileTag> GetTagsByName(string tagName)
        {
            return FileTagList.Where(x => x.TagName.StartsWith(tagName, StringComparison.OrdinalIgnoreCase));
        }

        public override bool ImportSettings(object import)
        {
            if (import is string importString)
            {
                FileTagList = jsonSettingsSerializer.DeserializeFromJson<List<IFileTag>>(importString);
            }
            else if (import is List<IFileTag> importList)
            {
                FileTagList = importList;
            }

            FileTagList ??= s_defaultFileTags;

            if (FileTagList != null)
            {
                FlushSettings();
                OnSettingImportedEvent?.Invoke(this, null);
                return true;
            }

            return false;
        }

        public override object ExportSettings()
        {
            // Return string in Json format
            return jsonSettingsSerializer.SerializeToJson(FileTagList);
        }

        public void ReportToAppCenter()
        {
        }
    }
}
