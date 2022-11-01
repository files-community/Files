using Files.App.UserControls.Widgets;
using Files.App.ViewModels.Widgets;
using Files.Backend.Services.Settings;
using System.Collections.Generic;

namespace Files.App.Helpers
{
	public static class WidgetsHelpers
	{
		public static TWidget? TryGetWidget<TWidget>(IAppearanceSettingsService appearanceSettingsService, WidgetsListControlViewModel widgetsViewModel, out bool shouldReload, TWidget? defaultValue = default) where TWidget : IWidgetItemModel, new()
		{
			bool canAddWidget = widgetsViewModel.CanAddWidget(typeof(TWidget).Name);
			bool isWidgetSettingEnabled = TryGetIsWidgetSettingEnabled<TWidget>(appearanceSettingsService);

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

		public static bool TryGetIsWidgetSettingEnabled<TWidget>(IAppearanceSettingsService appearanceSettingsService) where TWidget : IWidgetItemModel
		{
			if (typeof(TWidget) == typeof(FolderWidget))
			{
				return appearanceSettingsService.ShowFoldersWidget;
			}
			if (typeof(TWidget) == typeof(DrivesWidget))
			{
				return appearanceSettingsService.ShowDrivesWidget;
			}
			if (typeof(TWidget) == typeof(BundlesWidget))
			{
				return appearanceSettingsService.ShowBundlesWidget;
			}
			if (typeof(TWidget) == typeof(RecentFilesWidget))
			{
				return appearanceSettingsService.ShowRecentFilesWidget;
			}
			// A custom widget it is - TWidget implements ICustomWidgetItemModel
			if (typeof(ICustomWidgetItemModel).IsAssignableFrom(typeof(TWidget)))
			{
				// Return true for custom widgets - they're always enabled
				return true;
			}

			return false;
		}
	}
}