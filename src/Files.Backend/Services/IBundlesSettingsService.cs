using Files.Backend.Services.Settings;
using System;
using System.Collections.Generic;

namespace Files.Backend.Services
{
    public interface IBundlesSettingsService : IBaseSettingsService
    {
        event EventHandler OnSettingImportedEvent;

        bool FlushSettings();

        object ExportSettings();

        bool ImportSettings(object import);

        Dictionary<string, List<string>> SavedBundles { get; set; }
    }
}
