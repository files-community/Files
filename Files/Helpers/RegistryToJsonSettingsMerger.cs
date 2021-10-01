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
                    userSettingsService.FilesAndFoldersSettingsService.ShowFileExtensions = appSettings.Get(true, "ShowFileExtensions");
                    userSettingsService.FilesAndFoldersSettingsService.AreHiddenItemsVisible = appSettings.Get(false, "AreHiddenItemsVisible");
                    userSettingsService.FilesAndFoldersSettingsService.AreSystemItemsHidden = appSettings.Get(true, "AreSystemItemsHidden");
                    userSettingsService.FilesAndFoldersSettingsService.ListAndSortDirectoriesAlongsideFiles = appSettings.Get(false, "ListAndSortDirectoriesAlongsideFiles");
                    userSettingsService.FilesAndFoldersSettingsService.OpenItemsWithOneclick = appSettings.Get(false, "OpenItemsWithOneclick");
                    userSettingsService.FilesAndFoldersSettingsService.SearchUnindexedItems = appSettings.Get(false, "SearchUnindexedItems");
                    userSettingsService.FilesAndFoldersSettingsService.AreLayoutPreferencesPerFolder = appSettings.Get(true, "AreLayoutPreferencesPerFolder");
                    userSettingsService.FilesAndFoldersSettingsService.AdaptiveLayoutEnabled = appSettings.Get(true, "AdaptiveLayoutEnabled");
                    userSettingsService.FilesAndFoldersSettingsService.AreFileTagsEnabled = appSettings.Get(false, "AreFileTagsEnabled");

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
                    userSettingsService.SidebarSettingsService.SidebarWidth = appSettings.Get(255d, "SidebarWidth");
                    userSettingsService.SidebarSettingsService.IsSidebarOpen = appSettings.Get(true, "IsSidebarOpen");
                    userSettingsService.SidebarSettingsService.ShowFavoritesSection = appSettings.Get(true, "ShowFavoritesSection");
                    userSettingsService.SidebarSettingsService.ShowLibrarySection = appSettings.Get(false, "ShowLibrarySection");
                    userSettingsService.SidebarSettingsService.ShowDrivesSection = appSettings.Get(true, "ShowDrivesSection");
                    userSettingsService.SidebarSettingsService.ShowCloudDrivesSection = appSettings.Get(true, "ShowCloudDrivesSection");
                    userSettingsService.SidebarSettingsService.ShowNetworkDrivesSection = appSettings.Get(true, "ShowNetworkDrivesSection");
                    userSettingsService.SidebarSettingsService.ShowWslSection = appSettings.Get(true, "ShowWslSection");
                    userSettingsService.SidebarSettingsService.PinRecycleBinToSidebar = appSettings.Get(true, "PinRecycleBinToSideBar");

                    // Preferences
                    userSettingsService.PreferencesSettingsService.ShowConfirmDeleteDialog = appSettings.Get(true, "ShowConfirmDeleteDialog");
                    userSettingsService.PreferencesSettingsService.OpenFoldersInNewTab = appSettings.Get(false, "OpenFoldersNewTab");

                    // Appearance
                    userSettingsService.AppearanceSettingsService.MoveOverflowMenuItemsToSubMenu = appSettings.Get(true, "MoveOverflowMenuItemsToSubMenu");

                    // Startup
                    userSettingsService.StartupSettingsService.OpenSpecificPageOnStartup = appSettings.Get(false, "OpenASpecificPageOnStartup");
                    userSettingsService.StartupSettingsService.OpenSpecificPageOnStartupPath = appSettings.Get("", "OpenASpecificPageOnStartupPath");
                    userSettingsService.StartupSettingsService.ContinueLastSessionOnStartUp = appSettings.Get(false, "ContinueLastSessionOnStartUp");
                    userSettingsService.StartupSettingsService.OpenNewTabOnStartup = appSettings.Get(true, "OpenNewTabPageOnStartup");
                    userSettingsService.StartupSettingsService.AlwaysOpenNewInstance = appSettings.Get(false, "AlwaysOpenANewInstance");
                    userSettingsService.StartupSettingsService.TabsOnStartupList = appSettings.Get<string[]>(null, "PagesOnStartupList")?.ToList();
                    userSettingsService.StartupSettingsService.LastSessionTabList = appSettings.Get<string[]>(null, "LastSessionPages")?.ToList();

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
