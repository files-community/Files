// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.AppCenter.Analytics;

namespace Files.App.Services.Settings
{
	internal sealed class AppearanceSettingsService : BaseObservableJsonSettings, IAppearanceSettingsService
	{
		public AppearanceSettingsService(ISettingsSharingContext settingsSharingContext)
		{
			// Register root
			RegisterSettingsContext(settingsSharingContext);
		}

		public double SidebarWidth
		{
			get => Get(Math.Min(Math.Max(Get(255d), Constants.UI.MinimumSidebarWidth), 500d));
			set => Set(value);
		}

		public bool IsSidebarOpen
		{
			get => Get(true);
			set => Set(value);
		}

		/// <inheritdoc/>
		public String AppThemeBackgroundColor
		{
			get => Get("#00000000");
			set => Set(value);
		}

		/// <inheritdoc/>
		public String AppThemeAddressBarBackgroundColor
		{
			get => Get("");
			set => Set(value);
		}

		/// <inheritdoc/>
		public String AppThemeSidebarBackgroundColor
		{
			get => Get("");
			set => Set(value);
		}

		/// <inheritdoc/>
		public String AppThemeFileAreaBackgroundColor
		{
			get => Get("");
			set => Set(value);
		}

		/// <inheritdoc/>
		public String AppThemeFontFamily
		{
			get => Get("Segoe UI Variable");
			set => Set(value);
		}

		/// <inheritdoc/>
		public BackdropMaterialType AppThemeBackdropMaterial
		{
			get => Get(BackdropMaterialType.MicaAlt);
			set => Set(value);
		}

		protected override void RaiseOnSettingChangedEvent(object sender, SettingChangedEventArgs e)
		{
			switch (e.SettingName)
			{
				case nameof(AppThemeBackgroundColor):
				case nameof(AppThemeAddressBarBackgroundColor):
				case nameof(AppThemeSidebarBackgroundColor):
				case nameof(AppThemeFileAreaBackgroundColor):
				case nameof(AppThemeBackdropMaterial):
					Analytics.TrackEvent($"Set {e.SettingName} to {e.NewValue}");
					break;
			}

			base.RaiseOnSettingChangedEvent(sender, e);
		}
	}
}
