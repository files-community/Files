using System.Collections.Generic;

namespace Files.SettingsInterfaces
{
    public interface IBundlesSettings
    {
        Dictionary<string, List<string>> SavedBundles { get; set; }

        object ExportSettings();

        void ImportSettings(object import);
    }
}