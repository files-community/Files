using System.Collections.Generic;

namespace Files.SettingsInterfaces
{
    public interface IJsonSettings
    {
        Dictionary<string, List<string>> SavedBundles { get; set; }
    }
}
