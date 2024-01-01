// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Helpers
{
	/// <summary>
	/// Provides static helper for Widgets, which are shown in the <see cref="HomePage"/>.
	/// </summary>
	public static class WidgetsHelpers
	{
		private static IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();

		public static TWidget? TryGetWidget<TWidget>(HomeViewModel widgetsViewModel, out bool shouldReload, TWidget? defaultValue = default)
			where TWidget : IWidgetViewModel, new()
		{
			bool canAddWidget = widgetsViewModel.CanAddWidget(typeof(TWidget).Name);
			bool isWidgetSettingEnabled = TryGetIsWidgetSettingEnabled<TWidget>();

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

			return (defaultValue);
		}

		public static bool TryGetIsWidgetSettingEnabled<TWidget>()
			where TWidget : IWidgetViewModel
		{
			return typeof(TWidget).Name switch
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
