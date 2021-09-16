using Files.Services;
using Files.ViewModels;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Files.Helpers
{
    // TODO: Please remove this class after V2 version next since most of users would have merged the settings by that time
    public class RegistryToJsonSettingsMerger
    {
        public static void MergeSettings()
        {
            if (!App.AppSettings.AreRegistrySettingsMergedToJson)
            {
                IUserSettingsService userSettingsService = Ioc.Default.GetService<IUserSettingsService>();
                SettingsViewModel appSettings = App.AppSettings;

                try
                {
                    userSettingsService.PreviewPaneSizeHorizontalPx = appSettings.PreviewPaneSizeHorizontal.Value;
                    userSettingsService.PreviewPaneSizeVerticalPx = appSettings.PreviewPaneSizeVertical.Value;
                    userSettingsService.PreviewPaneEnabled = appSettings.PreviewPaneEnabled;
                    userSettingsService.ShowPreviewOnly = appSettings.ShowPreviewOnly;

                    // Files and folders
                    userSettingsService.FilesAndFoldersSettingsService.ShowFileExtensions = appSettings.ShowFileExtensions;
                    userSettingsService.FilesAndFoldersSettingsService.AreHiddenItemsVisible = appSettings.AreHiddenItemsVisible;
                    userSettingsService.FilesAndFoldersSettingsService.AreSystemItemsHidden = appSettings.AreSystemItemsHidden;
                    userSettingsService.FilesAndFoldersSettingsService.ListAndSortDirectoriesAlongsideFiles = appSettings.ListAndSortDirectoriesAlongsideFiles;
                    userSettingsService.FilesAndFoldersSettingsService.OpenItemsWithOneclick = appSettings.OpenItemsWithOneclick;
                    userSettingsService.FilesAndFoldersSettingsService.SearchUnindexedItems = appSettings.SearchUnindexedItems;
                    userSettingsService.FilesAndFoldersSettingsService.AreLayoutPreferencesPerFolder = appSettings.AreLayoutPreferencesPerFolder;
                    userSettingsService.FilesAndFoldersSettingsService.AdaptiveLayoutEnabled = appSettings.AdaptiveLayoutEnabled;
                    userSettingsService.FilesAndFoldersSettingsService.AreFileTagsEnabled = appSettings.AreFileTagsEnabled;

                    // Multitasking
                    userSettingsService.MultitaskingSettingsService.IsVerticalTabFlyoutEnabled = appSettings.IsVerticalTabFlyoutEnabled;
                    userSettingsService.MultitaskingSettingsService.IsDualPaneEnabled = appSettings.IsDualPaneEnabled;
                    userSettingsService.MultitaskingSettingsService.AlwaysOpenDualPaneInNewTab = appSettings.AlwaysOpenDualPaneInNewTab;

                    // Widgets
                    userSettingsService.WidgetsSettingsService.ShowFoldersWidget = appSettings.ShowFolderWidgetWidget;
                    userSettingsService.WidgetsSettingsService.ShowRecentFilesWidget = appSettings.ShowRecentFilesWidget;
                    userSettingsService.WidgetsSettingsService.ShowDrivesWidget = appSettings.ShowDrivesWidget;
                    userSettingsService.WidgetsSettingsService.ShowBundlesWidget = appSettings.ShowBundlesWidget;

                    // Sidebar
                    userSettingsService.SidebarSettingsService.SidebarWidth = appSettings.SidebarWidth.Value;
                    userSettingsService.SidebarSettingsService.IsSidebarOpen = appSettings.IsSidebarOpen;
                    userSettingsService.SidebarSettingsService.ShowFavoritesSection = appSettings.ShowFavoritesSection;
                    userSettingsService.SidebarSettingsService.ShowLibrarySection = appSettings.ShowLibrarySection;
                    userSettingsService.SidebarSettingsService.ShowDrivesSection = appSettings.ShowDrivesSection;
                    userSettingsService.SidebarSettingsService.ShowCloudDrivesSection = appSettings.ShowCloudDrivesSection;
                    userSettingsService.SidebarSettingsService.ShowNetworkDrivesSection = appSettings.ShowNetworkDrivesSection;
                    userSettingsService.SidebarSettingsService.ShowWslSection = appSettings.ShowWslSection;
                    userSettingsService.SidebarSettingsService.PinRecycleBinToSideBar = appSettings.PinRecycleBinToSideBar;

                    // Preferences
                    userSettingsService.PreferencesSettingsService.ShowConfirmDeleteDialog = appSettings.ShowConfirmDeleteDialog;
                    userSettingsService.PreferencesSettingsService.OpenFoldersInNewTab = appSettings.OpenFoldersNewTab;

                    // Appearance
                    userSettingsService.AppearanceSettingsService.MoveOverflowMenuItemsToSubMenu = appSettings.MoveOverflowMenuItemsToSubMenu;

                    // Startup
                    userSettingsService.StartupSettingsService.OpenSpecificPageOnStartup = appSettings.OpenASpecificPageOnStartup;
                    userSettingsService.StartupSettingsService.OpenSpecificPageOnStartupPath = appSettings.OpenASpecificPageOnStartupPath;
                    userSettingsService.StartupSettingsService.ContinueLastSessionOnStartUp = appSettings.ContinueLastSessionOnStartUp;
                    userSettingsService.StartupSettingsService.OpenNewTabOnStartup = appSettings.OpenNewTabPageOnStartup;
                    userSettingsService.StartupSettingsService.AlwaysOpenNewInstance = appSettings.AlwaysOpenANewInstance;
                    userSettingsService.StartupSettingsService.TabsOnStartupList = appSettings.PagesOnStartupList.ToList();
                    userSettingsService.StartupSettingsService.LastSessionTabList = appSettings.LastSessionPages.ToList();

                    App.AppSettings.AreRegistrySettingsMergedToJson = true;
                }
                catch (Exception ex)
                {
                    App.Logger.Error(ex);
                    Debugger.Break();
                }
            }
        }
    }
}
