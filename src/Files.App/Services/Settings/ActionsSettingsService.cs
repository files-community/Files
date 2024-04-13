// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Utils.Serialization.Implementation;
using Windows.Storage;

namespace Files.App.Services.Settings
{
	internal class ActionsSettingsService : BaseJsonSettings, IActionsSettingsService
	{
		public List<ActionWithCustomArgItem>? Actions
		{
			get => Get<List<ActionWithCustomArgItem>>(null) ?? [];
			set => Set(value);
		}

		public ActionsSettingsService()
		{
			SettingsSerializer = new DefaultSettingsSerializer();
			JsonSettingsSerializer = new DefaultJsonSettingsSerializer();
			JsonSettingsDatabase = new CachingJsonSettingsDatabase(SettingsSerializer, JsonSettingsSerializer);

			Initialize(SystemIO.Path.Combine(ApplicationData.Current.LocalFolder.Path,
				Constants.LocalSettings.SettingsFolderName, Constants.LocalSettings.ActionsSettingsFileName));
		}
	}
}
