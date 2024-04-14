// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.AppCenter.Analytics;

namespace Files.App.Services.Settings
{
	internal sealed class ActionsSettingsService : BaseObservableJsonSettings, IActionsSettingsService
	{
		/// <inheritdoc/>
		public List<ActionWithParameterItem>? Actions
		{
			get => Get<List<ActionWithParameterItem>>(null);
			set => Set(value);
		}

		public ActionsSettingsService(ISettingsSharingContext settingsSharingContext)
		{
			// Register root
			RegisterSettingsContext(settingsSharingContext);
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
