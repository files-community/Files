using Files.Backend.ViewModels.FileTags;
using System;
using System.Collections.Generic;

namespace Files.Backend.Services.Settings
{
    public interface IFileTagsSettingsService : IBaseSettingsService
    {
        event EventHandler OnSettingImportedEvent;

        IList<FileTagViewModel> FileTagList { get; set; }

        FileTagViewModel GetTagById(string uid);

        IList<FileTagViewModel> GetTagsByIds(string[] uids);

        IEnumerable<FileTagViewModel> GetTagsByName(string tagName);

        IEnumerable<FileTagViewModel> SearchTagsByName(string tagName);

        object ExportSettings();

        bool ImportSettings(object import);
    }
}