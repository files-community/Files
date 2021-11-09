using Files.Enums;
using Files.Services;
using Files.ViewModels;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using System;
using System.Diagnostics;
using System.Linq;

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
                    // Preview pane
                    userSettingsService.PreviewPaneSettingsService.PreviewPaneSizeHorizontalPx = appSettings.Get(300d, "PreviewPaneSizeHorizontal");
                    userSettingsService.PreviewPaneSettingsService.PreviewPaneSizeVerticalPx = appSettings.Get(250d, "PreviewPaneSizeVertical");
                    userSettingsService.PreviewPaneSettingsService.PreviewPaneEnabled = appSettings.Get(false, "PreviewPaneEnabled");
                    userSettingsService.PreviewPaneSettingsService.ShowPreviewOnly = appSettings.Get(false, "ShowPreviewOnly");
                    userSettingsService.PreviewPaneSettingsService.PreviewPaneMediaVolume = appSettings.Get(1.0d, "MediaVolume");

                    // Files and folders
                    userSettingsService.PreferencesSettingsService.ShowFileExtensions = appSettings.Get(true, "ShowFileExtensions");
                    userSettingsService.PreferencesSettingsService.AreHiddenItemsVisible = appSettings.Get(false, "AreHiddenItemsVisible");
                    userSettingsService.PreferencesSettingsService.AreSystemItemsHidden = appSettings.Get(true, "AreSystemItemsHidden");
                    userSettingsService.PreferencesSettingsService.ListAndSortDirectoriesAlongsideFiles = appSettings.Get(false, "ListAndSortDirectoriesAlongsideFiles");
                    userSettingsService.PreferencesSettingsService.OpenFilesWithOneClick = appSettings.Get(false, "OpenItemsWithOneClick");
                    userSettingsService.PreferencesSettingsService.OpenFoldersWithOneClick = appSettings.Get(false, "OpenItemsWithOneClick");
                    userSettingsService.PreferencesSettingsService.SearchUnindexedItems = appSettings.Get(false, "SearchUnindexedItems");
                    userSettingsService.PreferencesSettingsService.AreLayoutPreferencesPerFolder = appSettings.Get(true, "AreLayoutPreferencesPerFolder");
                    userSettingsService.PreferencesSettingsService.AdaptiveLayoutEnabled = appSettings.Get(true, "AdaptiveLayoutEnabled");
                    userSettingsService.PreferencesSettingsService.AreFileTagsEnabled = appSettings.Get(false, "AreFileTagsEnabled");

                    // Multitasking
                    userSettingsService.MultitaskingSettingsService.IsVerticalTabFlyoutEnabled = appSettings.Get(true, "IsVerticalTabFlyoutEnabled");
                    userSettingsService.MultitaskingSettingsService.IsDualPaneEnabled = appSettings.Get(false, "IsDualPaneEnabled");
                    userSettingsService.MultitaskingSettingsService.AlwaysOpenDualPaneInNewTab = appSettings.Get(false, "AlwaysOpenDualPaneInNewTab");

                    // Widgets
                    userSettingsService.WidgetsSettingsService.ShowFoldersWidget = appSettings.Get(true, "ShowFolderWidgetWidget");
                    userSettingsService.WidgetsSettingsService.ShowRecentFilesWidget = appSettings.Get(true, "ShowRecentFilesWidget");
                    userSettingsService.WidgetsSettingsService.ShowDrivesWidget = appSettings.Get(true, "ShowDrivesWidget");
                    userSettingsService.WidgetsSettingsService.ShowBundlesWidget = appSettings.Get(false, "ShowBundlesWidget");

                    // Sidebar
                    userSettingsService.AppearanceSettingsService.SidebarWidth = appSettings.Get(255d, "SidebarWidth");
                    userSettingsService.AppearanceSettingsService.IsSidebarOpen = appSettings.Get(true, "IsSidebarOpen");
                    userSettingsService.AppearanceSettingsService.ShowFavoritesSection = appSettings.Get(true, "ShowFavoritesSection");
                    userSettingsService.AppearanceSettingsService.ShowLibrarySection = appSettings.Get(false, "ShowLibrarySection");
                    userSettingsService.AppearanceSettingsService.ShowDrivesSection = appSettings.Get(true, "ShowDrivesSection");
                    userSettingsService.AppearanceSettingsService.ShowCloudDrivesSection = appSettings.Get(true, "ShowCloudDrivesSection");
                    userSettingsService.AppearanceSettingsService.ShowNetworkDrivesSection = appSettings.Get(true, "ShowNetworkDrivesSection");
                    userSettingsService.AppearanceSettingsService.ShowWslSection = appSettings.Get(true, "ShowWslSection");
                    userSettingsService.AppearanceSettingsService.PinRecycleBinToSidebar = appSettings.Get(true, "PinRecycleBinToSideBar");

                    // Preferences
                    userSettingsService.PreferencesSettingsService.ShowConfirmDeleteDialog = appSettings.Get(true, "ShowConfirmDeleteDialog");
                    userSettingsService.PreferencesSettingsService.OpenFoldersInNewTab = appSettings.Get(false, "OpenFoldersNewTab");

                    // Appearance
                    userSettingsService.AppearanceSettingsService.MoveOverflowMenuItemsToSubMenu = appSettings.Get(true, "MoveOverflowMenuItemsToSubMenu");

                    // Startup
                    userSettingsService.PreferencesSettingsService.OpenSpecificPageOnStartup = appSettings.Get(false, "OpenASpecificPageOnStartup");
                    userSettingsService.PreferencesSettingsService.OpenSpecificPageOnStartupPath = appSettings.Get("", "OpenASpecificPageOnStartupPath");
                    userSettingsService.PreferencesSettingsService.ContinueLastSessionOnStartUp = appSettings.Get(false, "ContinueLastSessionOnStartUp");
                    userSettingsService.PreferencesSettingsService.OpenNewTabOnStartup = appSettings.Get(true, "OpenNewTabPageOnStartup");
                    userSettingsService.PreferencesSettingsService.AlwaysOpenNewInstance = appSettings.Get(false, "AlwaysOpenANewInstance");
                    userSettingsService.PreferencesSettingsService.TabsOnStartupList = appSettings.Get<string[]>(null, "PagesOnStartupList")?.ToList();
                    userSettingsService.PreferencesSettingsService.LastSessionTabList = appSettings.Get<string[]>(null, "LastSessionPages")?.ToList();

                    // Layout settings
                    userSettingsService.LayoutSettingsService.ShowDateColumn = appSettings.Get(true, "ShowDateColumn");
                    userSettingsService.LayoutSettingsService.ShowDateCreatedColumn = appSettings.Get(false, "ShowDateCreatedColumn");
                    userSettingsService.LayoutSettingsService.ShowTypeColumn = appSettings.Get(true, "ShowTypeColumn");
                    userSettingsService.LayoutSettingsService.ShowSizeColumn = appSettings.Get(true, "ShowSizeColumn");
                    userSettingsService.LayoutSettingsService.ShowFileTagColumn = appSettings.Get(true, "ShowFileTagColumn");
                    userSettingsService.LayoutSettingsService.DefaultGridViewSize = appSettings.Get(Constants.Browser.GridViewBrowser.GridViewSizeSmall, "DefaultGridViewSize");
                    userSettingsService.LayoutSettingsService.DefaultLayoutMode = (FolderLayoutModes)appSettings.Get((byte)FolderLayoutModes.DetailsView, "DefaultLayoutMode");
                    userSettingsService.LayoutSettingsService.DefaultDirectorySortDirection = (SortDirection)appSettings.Get((byte)Microsoft.Toolkit.Uwp.UI.SortDirection.Ascending, "DefaultDirectorySortDirection");
                    userSettingsService.LayoutSettingsService.DefaultDirectorySortOption = (SortOption)appSettings.Get((byte)SortOption.Name, "DefaultDirectorySortOption");
                    userSettingsService.LayoutSettingsService.DefaultDirectoryGroupOption = (GroupOption)appSettings.Get((byte)GroupOption.None, "DefaultDirectoryGroupOption");

                    App.AppSettings.AreRegistrySettingsMergedToJson = true;
                }
                catch (Exception ex)
                {
                    App.Logger.Warn(ex, "Merging settings failed");
                    Debugger.Break();
                }
            }
        }
    }
}
