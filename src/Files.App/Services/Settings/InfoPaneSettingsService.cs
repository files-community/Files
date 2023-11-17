// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Utils.Serialization;
using Files.Core.Services.Settings;
using Microsoft.AppCenter.Analytics;
using System;

namespace Files.App.Services.Settings
{
	internal sealed class InfoPaneSettingsService : BaseObservableJsonSettings, IInfoPaneSettingsService
	{
		public bool IsEnabled
		{
			get => Get(false);
			set => Set(value);
		}

		public double HorizontalSizePx
		{
			get => Math.Max(100d, Get(300d));
			set => Set(Math.Max(100d, value));
		}

		public double VerticalSizePx
		{
			get => Math.Max(100d, Get(250d));
			set => Set(Math.Max(100d, value));
		}

		public double MediaVolume
		{
			get => Math.Min(Math.Max(Get(1d), 0d), 1d);
			set => Set(Math.Max(0d, Math.Min(value, 1d)));
		}

		public InfoPaneTabs SelectedTab
		{
			get => Get(InfoPaneTabs.Details);
			set => Set(value);
		}

		public InfoPaneSettingsService(ISettingsSharingContext settingsSharingContext)
		{
			RegisterSettingsContext(settingsSharingContext);
		}

		protected override void RaiseOnSettingChangedEvent(object sender, SettingChangedEventArgs e)
		{
			if (e.SettingName is nameof(SelectedTab))
			{
				Analytics.TrackEvent($"Set {e.SettingName} to {e.NewValue}");
			}

			base.RaiseOnSettingChangedEvent(sender, e);
		}
	}
}
