using System.Collections.Generic;

namespace Files.Settings
{
    public interface IJsonSettings
    {
        Dictionary<string, List<string>> SavedBundles { get; set; }
    }
}
