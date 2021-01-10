using System.Collections.Generic;

namespace Files.SettingsInterfaces
{
    public interface IWidgetsSettings
    {
        Dictionary<string, List<string>> SavedBundles { get; set; }
    }
}
