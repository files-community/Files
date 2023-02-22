using System;
using System.Collections.Generic;

namespace Files.Core.Services.Settings
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
