// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.UserControls.Widgets;

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
				nameof(QuickAccessWidget) => UserSettingsService.GeneralSettingsService.ShowQuickAccessWidget,
				nameof(DrivesWidget) => UserSettingsService.GeneralSettingsService.ShowDrivesWidget,
				nameof(FileTagsWidget) => UserSettingsService.GeneralSettingsService.ShowFileTagsWidget,
				nameof(RecentFilesWidget) => UserSettingsService.GeneralSettingsService.ShowRecentFilesWidget,
				_ => false,
			};
		}
	}
}
