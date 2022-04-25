using Files.Backend.Services.Settings;
using Files.Uwp.Filesystem;
using Files.Uwp.UserControls.Widgets;
using Files.Uwp.UserControls.Widgets;
using Files.Uwp.ViewModels.Widgets;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Files.Uwp.Helpers
{
    public static class WidgetsHelpers
    {
        public static TWidget TryGetWidget<TWidget>(IWidgetsSettingsService widgetsSettingsService, WidgetsListControlViewModel widgetsViewModel, out bool shouldReload, TWidget defaultValue = default) where TWidget : IWidgetItemModel, new()
        {
            bool canAddWidget = widgetsViewModel.CanAddWidget(typeof(TWidget).Name);
            bool isWidgetSettingEnabled = TryGetIsWidgetSettingEnabled<TWidget>(widgetsSettingsService);

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

        public static bool TryGetIsWidgetSettingEnabled<TWidget>(IWidgetsSettingsService widgetsSettingsService) where TWidget : IWidgetItemModel
        {
            if (typeof(TWidget) == typeof(FolderWidget))
            {
                return widgetsSettingsService.ShowFoldersWidget;
            }
            if (typeof(TWidget) == typeof(DrivesWidget))
            {
                return widgetsSettingsService.ShowDrivesWidget;
            }
            if (typeof(TWidget) == typeof(BundlesWidget))
            {
                return widgetsSettingsService.ShowBundlesWidget;
            }
            if (typeof(TWidget) == typeof(RecentFilesWidget))
            {
                return widgetsSettingsService.ShowRecentFilesWidget;
            }
            // A custom widget it is - TWidget implements ICustomWidgetItemModel
            if (typeof(ICustomWidgetItemModel).IsAssignableFrom(typeof(TWidget)))
            {
                // Return true for custom widgets - they're always enabled
                return true;
            }

            return false;
        }

        public static class WidgetCards
        {
            /// <summary>
            /// Loads an icon for each IWidgetCardItem included
            /// </summary>
            /// <typeparam name="T">The type of IWidgetCardItem in the collection</typeparam>
            /// <typeparam name="U">The type of the INavigationControlItem backing every card item</typeparam>
            /// <param name="cardItems">A collection of widget card items to load thumbnails for</param>
            /// <returns></returns>
            public static async Task LoadCardIcons<T, U>(IList<T> cardItems)
                where T : IWidgetCardItem<U>
            {
                foreach (T cardItem in cardItems.ToList()) // ToList() is necessary
                {
                    await cardItem.LoadCardThumbnailAsync();
                }
            }
        }
    }
}