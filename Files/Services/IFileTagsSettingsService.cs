using Files.Filesystem;
using System.Collections.Generic;

namespace Files.Services
{
    public interface IFileTagsSettingsService
    {
        IList<FileTag> FileTagList { get; set; }

        FileTag GetTagById(string uid);

        IEnumerable<FileTag> GetTagsByName(string tagName);
    }
}
