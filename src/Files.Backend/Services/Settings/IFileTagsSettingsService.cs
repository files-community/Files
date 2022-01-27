using Files.Backend.Models;
using System;
using System.Collections.Generic;

namespace Files.Backend.Services.Settings
{
    public interface IFileTagsSettingsService : IBaseSettingsService
    {
        event EventHandler OnSettingImportedEvent;

        IList<IFileTag> FileTagList { get; set; }

        IFileTag GetTagById(string uid);

        IEnumerable<IFileTag> GetTagsByName(string tagName);

        object ExportSettings();

        bool ImportSettings(object import);
    }
}
