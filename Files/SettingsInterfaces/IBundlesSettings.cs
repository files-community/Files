using System.Collections.Generic;

namespace Files.SettingsInterfaces
{
    public interface IBundlesSettings
    {
        void FlushSettings();

        object ExportSettings();

        bool ImportSettings(object import);

        Dictionary<string, List<string>> SavedBundles { get; set; }
    }
}