using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using Windows.Storage;
using Windows.UI.Popups;

namespace Files.ViewModels.SettingsViewModels
{
    public class ImportAndExportViewModel : ObservableObject
    {
        private SettingsViewModel AppSettings = App.AppSettings;

        const string contanier = "FilesSettings";
        public RelayCommand ImportCommand => new RelayCommand(() => ImportSettings());
        public RelayCommand ExportCommand => new RelayCommand(() => ExportSettings());

        public async void ImportSettings()
        {
            ApplicationDataContainer roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;
            Windows.Storage.ApplicationDataCompositeValue composite = (ApplicationDataCompositeValue)roamingSettings.Values[contanier];
            bool result = false;

            if (composite != null)
            {
                try
                {
                    App.AppSettings.ClearSettings();

                    AppSettings.MoveOverflowMenuItemsToSubMenu = (bool)composite["MoveOverflowMenuItemsToSubMenu"];
                    AppSettings.SelectedTheme = new Helpers.AppTheme((string)composite["SelectedTheme.Name"], (string)composite["SelectedTheme.Path"], (string)composite["SelectedTheme.AbsolutePath"], (bool)composite["SelectedTheme.IsFromOptionalPackage"]);
                    AppSettings.AreFileTagsEnabled = (bool)composite["AreFileTagsEnabled"];
                    AppSettings.AreHiddenItemsVisible = (bool)composite["AreHiddenItemsVisible"];
                    AppSettings.AreSystemItemsHidden = (bool)composite["AreSystemItemsHidden"];
                    AppSettings.ShowFileExtensions = (bool)composite["ShowFileExtensions"];
                    AppSettings.OpenItemsWithOneclick = (bool)composite["OpenItemsWithOneclick"];
                    AppSettings.ListAndSortDirectoriesAlongsideFiles = (bool)composite["ListAndSortDirectoriesAlongsideFiles"];
                    AppSettings.SearchUnindexedItems = (bool)composite["SearchUnindexedItems"];
                    AppSettings.AreLayoutPreferencesPerFolder = (bool)composite["AreLayoutPreferencesPerFolder"];
                    AppSettings.IsVerticalTabFlyoutEnabled = (bool)composite["IsVerticalTabFlyoutEnabled"];
                    AppSettings.IsDualPaneEnabled = (bool)composite["IsDualPaneEnabled"];
                    AppSettings.AlwaysOpenDualPaneInNewTab = (bool)composite["AlwaysOpenDualPaneInNewTab"];
                    AppSettings.OpenNewTabPageOnStartup = (bool)composite["OpenNewTabPageOnStartup"];
                    AppSettings.ContinueLastSessionOnStartUp = (bool)composite["ContinueLastSessionOnStartUp"];
                    AppSettings.OpenASpecificPageOnStartup = (bool)composite["OpenASpecificPageOnStartup"];
                    AppSettings.AlwaysOpenANewInstance = (bool)composite["AlwaysOpenANewInstance"];
                    AppSettings.SidebarWidth = new Windows.UI.Xaml.GridLength((double)composite["SidebarWidth"], Windows.UI.Xaml.GridUnitType.Pixel);
                    AppSettings.IsSidebarOpen = (bool)composite["IsSidebarOpen"];
                    AppSettings.PreviewPaneSizeHorizontal = new Windows.UI.Xaml.GridLength((double)composite["PreviewPaneSizeHorizontal"], Windows.UI.Xaml.GridUnitType.Pixel);
                    AppSettings.PreviewPaneSizeVertical = new Windows.UI.Xaml.GridLength((double)composite["PreviewPaneSizeVertical"], Windows.UI.Xaml.GridUnitType.Pixel);
                    AppSettings.PreviewPaneEnabled = (bool)composite["PreviewPaneEnabled"];
                    AppSettings.ShowPreviewOnly = (bool)composite["ShowPreviewOnly"];
                    AppSettings.ShowConfirmDeleteDialog = (bool)composite["ShowConfirmDeleteDialog"];
                    AppSettings.ShowDrivesWidget = (bool)composite["ShowDrivesWidget"];
                    AppSettings.ShowLibrarySection = (bool)composite["ShowLibrarySection"];
                    AppSettings.ShowBundlesWidget = (bool)composite["ShowBundlesWidget"];
                    AppSettings.ShowDateColumn = (bool)composite["ShowDateColumn"];
                    AppSettings.ShowDateCreatedColumn = (bool)composite["ShowDateCreatedColumn"];
                    AppSettings.ShowTypeColumn = (bool)composite["ShowTypeColumn"];
                    AppSettings.ShowSizeColumn = (bool)composite["ShowSizeColumn"];
                    AppSettings.ShowFileTagColumn = (bool)composite["ShowFileTagColumn"];
                    AppSettings.DesktopPath = composite["DesktopPath"] as string;
                    AppSettings.DownloadsPath = composite["DownloadsPath"] as string;
                    AppSettings.TempPath = composite["TempPath"] as string;
                    AppSettings.HomePath = composite["HomePath"] as string;
                    AppSettings.RecycleBinPath = composite["RecycleBinPath"] as string;
                    AppSettings.NetworkFolderPath = composite["NetworkFolderPath"] as string;
                    AppSettings.AdaptiveLayoutEnabled = (bool)composite["AdaptiveLayoutEnabled"];
                    AppSettings.ShowFolderWidgetWidget = (bool)composite["ShowFolderWidgetWidget"];
                    AppSettings.ShowRecentFilesWidget = (bool)composite["ShowRecentFilesWidget"];
                    AppSettings.ShowFavoritesSection = (bool)composite["ShowFavoritesSection"];
                    AppSettings.ShowDrivesSection = (bool)composite["ShowDrivesSection"];
                    AppSettings.ShowCloudDrivesSection = (bool)composite["ShowCloudDrivesSection"];
                    AppSettings.ShowNetworkDrivesSection = (bool)composite["ShowNetworkDrivesSection"];
                    AppSettings.ShowWslSection = (bool)composite["ShowWslSection"];
                    AppSettings.OpenFoldersNewTab = (bool)composite["OpenFoldersNewTab"];
                    AppSettings.OpenASpecificPageOnStartupPath = composite["OpenASpecificPageOnStartupPath"] as string;
                    AppSettings.ShowOngoingTasksTeachingTip = (bool)composite["ShowOngoingTasksTeachingTip"];
                    AppSettings.ResumeAfterRestart = (bool)composite["ResumeAfterRestart"];
                    AppSettings.HideConfirmElevateDialog = (bool)composite["HideConfirmElevateDialog"];
                    AppSettings.MediaVolume = (double)composite["MediaVolume"];
                    AppSettings.DefaultGridViewSize = (int)composite["DefaultGridViewSize"];
                    AppSettings.LocalAppDataPath = composite["LocalAppDataPath"] as string;
                    AppSettings.DefaultLayoutMode = (Enums.FolderLayoutModes)composite["DefaultLayoutMode"];
                    AppSettings.DisplayedTimeStyle = (Enums.TimeStyle)composite["DisplayedTimeStyle"];
                    AppSettings.DefaultDirectoryGroupOption = (Enums.GroupOption)Enum.Parse(typeof(Enums.GroupOption), ((int)composite["DefaultDirectoryGroupOption"]).ToString());
                    AppSettings.DefaultDirectorySortOption = (Enums.SortOption)Enum.Parse(typeof(Enums.SortOption), ((int)composite["DefaultDirectorySortOption"]).ToString());
                    AppSettings.DefaultDirectorySortDirection = (Microsoft.Toolkit.Uwp.UI.SortDirection)Enum.Parse(typeof(Microsoft.Toolkit.Uwp.UI.SortDirection), ((int)composite["DefaultDirectorySortDirection"]).ToString());
                    AppSettings.CurrentLanguage.ID = composite["CurrentLanguage.ID"] as string;
                    AppSettings.CurrentLanguage.Name = composite["CurrentLanguage.Name"] as string;
                    AppSettings.PinRecycleBinToSideBar = (bool)composite["PinRecycleBinToSideBar"];

                    result = true;
                }
                catch (Exception)
                {
                    result = false;
                }
            }

            await new MessageDialog(result ? "Settings imported successfully." : "Error importing settings!").ShowAsync();
        }

        public async void ExportSettings()
        {
            ApplicationDataContainer roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;
            Windows.Storage.ApplicationDataCompositeValue composite = new Windows.Storage.ApplicationDataCompositeValue();
            bool result = false;

            try
            {
                composite["MoveOverflowMenuItemsToSubMenu"] = AppSettings.MoveOverflowMenuItemsToSubMenu;
                composite["SelectedTheme.AbsolutePath"] = AppSettings.SelectedTheme.AbsolutePath;
                composite["SelectedTheme.IsFromOptionalPackage"] = AppSettings.SelectedTheme.IsFromOptionalPackage;
                composite["SelectedTheme.Name"] = AppSettings.SelectedTheme.Name;
                composite["SelectedTheme.Path"] = AppSettings.SelectedTheme.Path;
                composite["AreFileTagsEnabled"] = AppSettings.AreFileTagsEnabled;
                composite["AreHiddenItemsVisible"] = AppSettings.AreHiddenItemsVisible;
                composite["AreSystemItemsHidden"] = AppSettings.AreSystemItemsHidden;
                composite["ShowFileExtensions"] = AppSettings.ShowFileExtensions;
                composite["OpenItemsWithOneclick"] = AppSettings.OpenItemsWithOneclick;
                composite["ListAndSortDirectoriesAlongsideFiles"] = AppSettings.ListAndSortDirectoriesAlongsideFiles;
                composite["SearchUnindexedItems"] = AppSettings.SearchUnindexedItems;
                composite["AreLayoutPreferencesPerFolder"] = AppSettings.AreLayoutPreferencesPerFolder;
                composite["IsVerticalTabFlyoutEnabled"] = AppSettings.IsVerticalTabFlyoutEnabled;
                composite["IsDualPaneEnabled"] = AppSettings.IsDualPaneEnabled;
                composite["AlwaysOpenDualPaneInNewTab"] = AppSettings.AlwaysOpenDualPaneInNewTab;
                composite["OpenNewTabPageOnStartup"] = AppSettings.OpenNewTabPageOnStartup;
                composite["ContinueLastSessionOnStartUp"] = AppSettings.ContinueLastSessionOnStartUp;
                composite["OpenASpecificPageOnStartup"] = AppSettings.OpenASpecificPageOnStartup;
                composite["AlwaysOpenANewInstance"] = AppSettings.AlwaysOpenANewInstance;
                composite["SidebarWidth"] = (double)AppSettings.SidebarWidth.Value;
                composite["IsSidebarOpen"] = AppSettings.IsSidebarOpen;
                composite["PreviewPaneSizeHorizontal"] = (double)AppSettings.PreviewPaneSizeHorizontal.Value;
                composite["PreviewPaneSizeVertical"] = (double)AppSettings.PreviewPaneSizeVertical.Value;
                composite["PreviewPaneEnabled"] = AppSettings.PreviewPaneEnabled;
                composite["ShowPreviewOnly"] = AppSettings.ShowPreviewOnly;
                composite["ShowConfirmDeleteDialog"] = AppSettings.ShowConfirmDeleteDialog;
                composite["ShowDrivesWidget"] = AppSettings.ShowDrivesWidget;
                composite["ShowLibrarySection"] = AppSettings.ShowLibrarySection;
                composite["ShowBundlesWidget"] = AppSettings.ShowBundlesWidget;
                composite["ShowDateColumn"] = AppSettings.ShowDateColumn;
                composite["ShowDateCreatedColumn"] = AppSettings.ShowDateCreatedColumn;
                composite["ShowTypeColumn"] = AppSettings.ShowTypeColumn;
                composite["ShowSizeColumn"] = AppSettings.ShowSizeColumn;
                composite["ShowFileTagColumn"] = AppSettings.ShowFileTagColumn;
                composite["DesktopPath"] = AppSettings.DesktopPath;
                composite["DownloadsPath"] = AppSettings.DownloadsPath;
                composite["TempPath"] = AppSettings.TempPath;
                composite["HomePath"] = AppSettings.HomePath;
                composite["RecycleBinPath"] = AppSettings.RecycleBinPath;
                composite["NetworkFolderPath"] = AppSettings.NetworkFolderPath;
                composite["AdaptiveLayoutEnabled"] = AppSettings.AdaptiveLayoutEnabled;
                composite["ShowFolderWidgetWidget"] = AppSettings.ShowFolderWidgetWidget;
                composite["ShowRecentFilesWidget"] = AppSettings.ShowRecentFilesWidget;
                composite["ShowFavoritesSection"] = AppSettings.ShowFavoritesSection;
                composite["ShowDrivesSection"] = AppSettings.ShowDrivesSection;
                composite["ShowCloudDrivesSection"] = AppSettings.ShowCloudDrivesSection;
                composite["ShowNetworkDrivesSection"] = AppSettings.ShowNetworkDrivesSection;
                composite["ShowWslSection"] = AppSettings.ShowWslSection;
                composite["OpenFoldersNewTab"] = AppSettings.OpenFoldersNewTab;
                composite["OpenASpecificPageOnStartupPath"] = AppSettings.OpenASpecificPageOnStartupPath;
                composite["ShowOngoingTasksTeachingTip"] = AppSettings.ShowOngoingTasksTeachingTip;
                composite["ResumeAfterRestart"] = AppSettings.ResumeAfterRestart;
                composite["HideConfirmElevateDialog"] = AppSettings.HideConfirmElevateDialog;
                composite["MediaVolume"] = AppSettings.MediaVolume;
                composite["DefaultGridViewSize"] = AppSettings.DefaultGridViewSize;
                composite["LocalAppDataPath"] = AppSettings.LocalAppDataPath;
                composite["DisplayedTimeStyle"] = ((int)(Enums.TimeStyle)AppSettings.DisplayedTimeStyle);
                composite["DefaultLayoutMode"] = ((int)(Enums.FolderLayoutModes)AppSettings.DefaultLayoutMode);
                composite["DefaultDirectoryGroupOption"] = ((int)(Enums.GroupOption)AppSettings.DefaultDirectoryGroupOption);
                composite["DefaultDirectorySortOption"] = ((int)(Enums.SortOption)AppSettings.DefaultDirectorySortOption);
                composite["DefaultDirectorySortDirection"] = ((int)(Microsoft.Toolkit.Uwp.UI.SortDirection)AppSettings.DefaultDirectorySortDirection);
                composite["CurrentLanguage.ID"] = AppSettings.CurrentLanguage.ID;
                composite["CurrentLanguage.Name"] = AppSettings.CurrentLanguage.Name;
                composite["PinRecycleBinToSideBar"] = AppSettings.PinRecycleBinToSideBar;

                roamingSettings.Values[contanier] = composite;

                result = true;
            }
            catch (Exception)
            {
                result = false;
            }

            await new MessageDialog(result ? "Settings exported successfully." : "Error exporting settings!").ShowAsync();
        }
    }
}