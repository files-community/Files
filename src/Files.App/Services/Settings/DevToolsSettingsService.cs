// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Services.Settings
{
	internal sealed partial class DevToolsSettingsService : BaseObservableJsonSettings, IDevToolsSettingsService
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

		/// <inheritdoc/>
		public string IDEPath
		{
			get => Get(SoftwareHelpers.IsVSCodeInstalled() ? "code" : string.Empty) ?? string.Empty;
			set => Set(value);
		}

		/// <inheritdoc/>
		public string IDEName
		{
			get => Get(SoftwareHelpers.IsVSCodeInstalled() ? Strings.VisualStudioCode.GetLocalizedResource() : string.Empty) ?? string.Empty;
			set => Set(value);
		}

		protected override void RaiseOnSettingChangedEvent(object sender, SettingChangedEventArgs e)
		{
			base.RaiseOnSettingChangedEvent(sender, e);
		}
	}
}
