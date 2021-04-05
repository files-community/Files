using Files.UserControls.Widgets;
using Files.ViewModels.Widgets;

namespace Files.Helpers
{
    public static class WidgetsHelpers
    {
        public static TWidget TryGetWidget<TWidget>(WidgetsListControlViewModel widgetsViewModel) where TWidget : IWidgetItemModel, new()
        {
            if (widgetsViewModel.CanAddWidget(typeof(TWidget).Name) && TryGetIsWidgetSettingEnabled<TWidget>())
            {
                return new TWidget();
            }

            return default(TWidget);
        }

        public static bool TryGetIsWidgetSettingEnabled<TWidget>() where TWidget : IWidgetItemModel
        {
            if (typeof(TWidget) == typeof(LibraryCards))
            {
                return App.AppSettings.ShowLibraryCardsWidget;
            }
            if (typeof(TWidget) == typeof(DrivesWidget))
            {
                return App.AppSettings.ShowDrivesWidget;
            }
            if (typeof(TWidget) == typeof(Bundles))
            {
                return App.AppSettings.ShowBundlesWidget;
            }
            if (typeof(TWidget) == typeof(RecentFiles))
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
