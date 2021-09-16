using System.Collections.Generic;

namespace Files.SettingsInterfaces
{
    public interface IBundlesSettings
    {
        bool FlushSettings();

        object ExportSettings();

        bool ImportSettings(object import);

        Dictionary<string, List<string>> SavedBundles { get; set; }
    }
}