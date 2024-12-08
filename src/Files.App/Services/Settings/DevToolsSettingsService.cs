// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

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
			base.RaiseOnSettingChangedEvent(sender, e);
		}
	}
}
