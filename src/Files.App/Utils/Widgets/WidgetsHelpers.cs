// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Services.Settings;

namespace Files.App.Helpers
{
	public static class WidgetsHelpers
	{
		private static IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();
		private static HomeViewModel HomeViewModel { get; } = Ioc.Default.GetRequiredService<HomeViewModel>();

		public static bool ShouldReloadWidgetItem<TWidgetViewModel>()
				where TWidgetViewModel : IWidgetViewModel, new()
		{
			bool canAddWidget = HomeViewModel.CanAddWidget(typeof(TWidgetViewModel).Name);
			bool isWidgetSettingEnabled = TryGetIsWidgetSettingEnabled<TWidgetViewModel>();

			if (canAddWidget && isWidgetSettingEnabled)
			{
				return true;
			}
			// The widgets exists but the setting has been disabled for it
			else if (!canAddWidget && !isWidgetSettingEnabled)
			{
				// Remove the widget
				HomeViewModel.RemoveWidget<TWidgetViewModel>();

				return false;
			}
			else if (!isWidgetSettingEnabled)
			{
				return false;
			}

			return false;
		}

		private static bool TryGetIsWidgetSettingEnabled<TWidgetViewModel>() where TWidgetViewModel : IWidgetViewModel
		{
			return typeof(TWidgetViewModel).Name switch
			{
				nameof(QuickAccessWidgetViewModel) => UserSettingsService.GeneralSettingsService.ShowQuickAccessWidget,
				nameof(DrivesWidgetViewModel) => UserSettingsService.GeneralSettingsService.ShowDrivesWidget,
				nameof(FileTagsWidgetViewModel) => UserSettingsService.GeneralSettingsService.ShowFileTagsWidget,
				nameof(RecentFilesWidgetViewModel) => UserSettingsService.GeneralSettingsService.ShowRecentFilesWidget,
				_ => false,
			};
		}
	}
}
