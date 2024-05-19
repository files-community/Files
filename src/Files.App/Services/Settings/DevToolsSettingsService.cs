// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.AppCenter.Analytics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Files.App.Services.Settings
{
	internal sealed class DevToolsSettingsService : BaseObservableJsonSettings, IDevToolsSettingsService
	{
		public DevToolsSettingsService(ISettingsSharingContext settingsSharingContext)
		{
			// Register root
			RegisterSettingsContext(settingsSharingContext);
		}

		/// <inheritdoc/>
		public OpenInIDEOption OpenInIDEOption
		{
			get => Get(OpenInIDEOption.GitRepos);
			set => Set(value);
		}

		protected override void RaiseOnSettingChangedEvent(object sender, SettingChangedEventArgs e)
		{
			switch (e.SettingName)
			{
				case nameof(OpenInIDEOption):
					Analytics.TrackEvent($"Set {e.SettingName} to {e.NewValue}");
					break;
			}

			base.RaiseOnSettingChangedEvent(sender, e);
		}
	}
}
