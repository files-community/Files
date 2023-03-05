using Files.App.UserControls.Widgets;
using Files.App.ViewModels.UserControls.Widgets;
using Files.Backend.Services.Settings;
using System.Collections.Generic;

namespace Files.App.Helpers
{
	public static class WidgetsHelpers
	{
		public static TWidget? TryGetWidget<TWidget>(IPreferencesSettingsService preferencesSettingsService, WidgetsListControlViewModel widgetsViewModel, out bool shouldReload, TWidget? defaultValue = default) where TWidget : IWidgetItemModel, new()
		{
			bool canAddWidget = widgetsViewModel.CanAddWidget(typeof(TWidget).Name);
			bool isWidgetSettingEnabled = TryGetIsWidgetSettingEnabled<TWidget>(preferencesSettingsService);

			if (canAddWidget && isWidgetSettingEnabled)
			{
				shouldReload = true;
				return new TWidget();
			}
			else if (!canAddWidget && !isWidgetSettingEnabled) // The widgets exists but the setting has been disabled for it
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

		public static bool TryGetIsWidgetSettingEnabled<TWidget>(IPreferencesSettingsService preferencesSettingsService) where TWidget : IWidgetItemModel
		{
			if (typeof(TWidget) == typeof(QuickAccessWidget))
			{
				return preferencesSettingsService.ShowQuickAccessWidget;
			}
			if (typeof(TWidget) == typeof(DrivesWidget))
			{
				return preferencesSettingsService.ShowDrivesWidget;
			}
			if (typeof(TWidget) == typeof(FileTagsWidget))
			{
				return preferencesSettingsService.ShowFileTagsWidget;
			}
			if (typeof(TWidget) == typeof(BundlesWidget))
			{
				return preferencesSettingsService.ShowBundlesWidget;
			}
			if (typeof(TWidget) == typeof(RecentFilesWidget))
			{
				return preferencesSettingsService.ShowRecentFilesWidget;
			}

			// A custom widget it is - TWidget implements ICustomWidgetItemModel
			return typeof(ICustomWidgetItemModel).IsAssignableFrom(typeof(TWidget)); // Return true for custom widgets - they're always enabled
		}
	}
}