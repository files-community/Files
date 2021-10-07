using System.Collections.Generic;

namespace Files.Services
{
    public interface IBundlesSettingsService
    {
        bool FlushSettings();

        object ExportSettings();

        bool ImportSettings(object import);

        Dictionary<string, List<string>> SavedBundles { get; set; }
    }
}
