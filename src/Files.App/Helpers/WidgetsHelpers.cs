// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.UserControls.Widgets;
using Files.App.ViewModels.Widgets;

namespace Files.App.Helpers
{
	public static class WidgetsHelpers
	{
		public static TWidget? TryGetWidget<TWidget>(IGeneralSettingsService generalSettingsService, WidgetsListControlViewModel widgetsViewModel, out bool shouldReload, TWidget? defaultValue = default) where TWidget : IWidgetItemModel, new()
		{
			bool canAddWidget = widgetsViewModel.CanAddWidget(typeof(TWidget).Name);
			bool isWidgetSettingEnabled = TryGetIsWidgetSettingEnabled<TWidget>(generalSettingsService);

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

		public static bool TryGetIsWidgetSettingEnabled<TWidget>(IGeneralSettingsService generalSettingsService) where TWidget : IWidgetItemModel
		{
			if (typeof(TWidget) == typeof(QuickAccessWidget))
				return generalSettingsService.ShowQuickAccessWidget;
			if (typeof(TWidget) == typeof(DrivesWidget))
				return generalSettingsService.ShowDrivesWidget;
			if (typeof(TWidget) == typeof(FileTagsWidget))
				return generalSettingsService.ShowFileTagsWidget;
			if (typeof(TWidget) == typeof(BundlesWidget))
				return generalSettingsService.ShowBundlesWidget;
			if (typeof(TWidget) == typeof(RecentFilesWidget))
				return generalSettingsService.ShowRecentFilesWidget;

			// A custom widget which is TWidget implements ICustomWidgetItemModel
			// Return true for custom widgets, which are always enabled
			return typeof(ICustomWidgetItemModel).IsAssignableFrom(typeof(TWidget));
		}
	}
}
