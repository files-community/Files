// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System;
using System.Collections.Generic;

namespace Files.Backend.Services.Settings
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
