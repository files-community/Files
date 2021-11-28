using Files.Filesystem;
using System;
using System.Collections.Generic;

namespace Files.Services
{
    public interface IFileTagsSettingsService
    {
        event EventHandler OnSettingImportedEvent;

        IList<FileTag> FileTagList { get; set; }

        FileTag GetTagById(string uid);

        IEnumerable<FileTag> GetTagsByName(string tagName);

        object ExportSettings();

        bool ImportSettings(object import);
    }
}
