// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Utils.Serialization.Implementation;
using Microsoft.AppCenter.Analytics;
using Windows.Storage;

namespace Files.App.Services.Settings
{
	internal sealed class ActionsSettingsService : BaseJsonSettings, IActionsSettingsService
	{
		/// <inheritdoc/>
		public List<ActionWithCustomArgItem>? Actions
		{
			get => Get<List<ActionWithCustomArgItem>>(null);
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

		protected override void RaiseOnSettingChangedEvent(object sender, SettingChangedEventArgs e)
		{
			switch (e.SettingName)
			{
				case nameof(Actions):
					Analytics.TrackEvent($"Set {e.SettingName} to {e.NewValue}");
					break;
			}

			base.RaiseOnSettingChangedEvent(sender, e);
		}
	}
}
