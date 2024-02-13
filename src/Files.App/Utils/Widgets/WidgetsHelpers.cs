// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.ViewModels.UserControls.Widgets;

namespace Files.App.Helpers
{
	public static class WidgetsHelpers
	{
		private static IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();

		public static TWidget? TryGetWidget<TWidget>(
			HomeViewModel widgetsViewModel,
			out bool shouldReload,
			TWidget? defaultValue = default)
				where TWidget : IWidgetViewModel, new()
		{
			bool canAddWidget = widgetsViewModel.CanAddWidget(typeof(TWidget).Name);
			bool isWidgetSettingEnabled = typeof(TWidget).ToString() switch
			{
				nameof(QuickAccessWidgetViewModel) => UserSettingsService.GeneralSettingsService.ShowQuickAccessWidget,
				nameof(DrivesWidgetViewModel) => UserSettingsService.GeneralSettingsService.ShowDrivesWidget,
				nameof(FileTagsWidgetViewModel) => UserSettingsService.GeneralSettingsService.ShowFileTagsWidget,
				nameof(RecentFilesWidgetViewModel) => UserSettingsService.GeneralSettingsService.ShowRecentFilesWidget,
				_ => false,
			};

			if (canAddWidget && isWidgetSettingEnabled)
			{
				shouldReload = true;
				return new TWidget();
			}
			// The widgets exists but the setting has been disabled for it
			else if (!canAddWidget && !isWidgetSettingEnabled)
			{
				// Remove the widget
				widgetsViewModel.RemoveWidget<TWidget>();
				shouldReload = false;

				return default;
			}
			else if (!isWidgetSettingEnabled)
			{
				shouldReload = false;

				return default;
			}

			shouldReload = EqualityComparer<TWidget>.Default.Equals(defaultValue, default);

			return defaultValue;
		}
	}
}
