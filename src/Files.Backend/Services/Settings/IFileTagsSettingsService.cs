using Files.Backend.ViewModels;
using System;
using System.Collections.Generic;

namespace Files.Backend.Services.Settings
{
    public interface IFileTagsSettingsService : IBaseSettingsService
    {
        event EventHandler OnSettingImportedEvent;

        IList<FileTagViewModel> FileTagList { get; set; }

        FileTagViewModel GetTagById(string uid);

        IEnumerable<FileTagViewModel> GetTagsByName(string tagName);

        object ExportSettings();

        bool ImportSettings(object import);
    }
}
