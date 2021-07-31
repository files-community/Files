using Files.UserControls.Widgets;
using Files.ViewModels.Widgets;
using System.Collections.Generic;

namespace Files.Helpers
{
    public static class WidgetsHelpers
    {
        public static TWidget TryGetWidget<TWidget>(WidgetsListControlViewModel widgetsViewModel, out bool shouldReload, TWidget defaultValue = default) where TWidget : IWidgetItemModel, new()
        {
            bool canAddWidget = widgetsViewModel.CanAddWidget(typeof(TWidget).Name);
            bool isWidgetSettingEnabled = TryGetIsWidgetSettingEnabled<TWidget>();

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

        public static bool TryGetIsWidgetSettingEnabled<TWidget>() where TWidget : IWidgetItemModel
        {
            if (typeof(TWidget) == typeof(FolderWidget))
            {
                return App.AppSettings.ShowFolderWidgetWidget;
            }
            if (typeof(TWidget) == typeof(DrivesWidget))
            {
                return App.AppSettings.ShowDrivesWidget;
            }
            if (typeof(TWidget) == typeof(BundlesWidget))
            {
                return App.AppSettings.ShowBundlesWidget;
            }
            if (typeof(TWidget) == typeof(RecentFilesWidget))
            {
                return App.AppSettings.ShowRecentFilesWidget;
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