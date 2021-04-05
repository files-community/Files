using Files.Dialogs;
using Files.Filesystem;
using Files.Helpers;
using Files.UserControls.Widgets;
using Files.ViewModels;
using Microsoft.Toolkit.Uwp;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Files.Views
{
    public sealed partial class YourHome : Page, IDisposable
    {
        private IShellPage AppInstance = null;
        public SettingsViewModel AppSettings => App.AppSettings;
        public FolderSettingsViewModel FolderSettings => AppInstance?.InstanceViewModel.FolderSettings;
        public NamedPipeAsAppServiceConnection Connection => AppInstance?.ServiceConnection;

        LibraryCards libraryCards;
        DrivesWidget drivesWidget;
        Bundles bundles;
        RecentFiles recentFiles;

        public YourHome()
        {
            InitializeComponent();
            this.Loaded += YourHome_Loaded;
        }

        private async void YourHome_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            libraryCards = WidgetsHelpers.TryGetWidget<LibraryCards>(Widgets.ViewModel, libraryCards);
            drivesWidget = WidgetsHelpers.TryGetWidget<DrivesWidget>(Widgets.ViewModel, drivesWidget);
            bundles = WidgetsHelpers.TryGetWidget<Bundles>(Widgets.ViewModel, bundles);
            recentFiles = WidgetsHelpers.TryGetWidget<RecentFiles>(Widgets.ViewModel, recentFiles);

            // Now prepare widgets
            if (libraryCards != null)
            {
                libraryCards.LibraryCardInvoked -= LibraryWidget_LibraryCardInvoked;
                libraryCards.LibraryCardNewPaneInvoked -= LibraryWidget_LibraryCardNewPaneInvoked;
                libraryCards.LibraryCardInvoked += LibraryWidget_LibraryCardInvoked;
                libraryCards.LibraryCardNewPaneInvoked += LibraryWidget_LibraryCardNewPaneInvoked;
                libraryCards.LibraryCardPropertiesInvoked -= LibraryWidget_LibraryCardPropertiesInvoked;
                libraryCards.LibraryCardPropertiesInvoked += LibraryWidget_LibraryCardPropertiesInvoked;
                libraryCards.LibraryCardDeleteInvoked -= LibraryWidget_LibraryCardDeleteInvoked;
                libraryCards.LibraryCardDeleteInvoked += LibraryWidget_LibraryCardDeleteInvoked;
                libraryCards.LibraryCardShowMultiPaneControlsInvoked -= LibraryCards_LibraryCardShowMultiPaneControlsInvoked;
                libraryCards.LibraryCardShowMultiPaneControlsInvoked += LibraryCards_LibraryCardShowMultiPaneControlsInvoked;

                Widgets.ViewModel.InsertWidget(libraryCards, 0);
            }
            if (drivesWidget != null)
            {
                drivesWidget.AppInstance = AppInstance;
                drivesWidget.DrivesWidgetInvoked -= DrivesWidget_DrivesWidgetInvoked;
                drivesWidget.DrivesWidgetNewPaneInvoked -= DrivesWidget_DrivesWidgetNewPaneInvoked;
                drivesWidget.DrivesWidgetInvoked += DrivesWidget_DrivesWidgetInvoked;
                drivesWidget.DrivesWidgetNewPaneInvoked += DrivesWidget_DrivesWidgetNewPaneInvoked;

                Widgets.ViewModel.InsertWidget(drivesWidget, 1);
            }
            if (bundles != null)
            {
                Widgets.ViewModel.InsertWidget(bundles, 2);

                bundles.ViewModel.Initialize(AppInstance);
                await bundles.ViewModel.Load();
            }
            if (recentFiles != null)
            {
                recentFiles.RecentFilesOpenLocationInvoked -= RecentFilesWidget_RecentFilesOpenLocationInvoked;
                recentFiles.RecentFileInvoked -= RecentFilesWidget_RecentFileInvoked;
                recentFiles.RecentFilesOpenLocationInvoked += RecentFilesWidget_RecentFilesOpenLocationInvoked;
                recentFiles.RecentFileInvoked += RecentFilesWidget_RecentFileInvoked;

                Widgets.ViewModel.InsertWidget(recentFiles, 3);
            }
        }

        private void LibraryCards_LibraryCardShowMultiPaneControlsInvoked(object sender, EventArgs e)
        {
            LibraryCards libraryCards = sender as LibraryCards;

            libraryCards.ShowMultiPaneControls = AppInstance.IsMultiPaneEnabled && AppInstance.IsPageMainPane;
        }

        private async void RecentFilesWidget_RecentFileInvoked(object sender, UserControls.PathNavigationEventArgs e)
        {
            try
            {
                var directoryName = Path.GetDirectoryName(e.ItemPath);
                await Win32Helpers.InvokeWin32ComponentAsync(e.ItemPath, AppInstance, workingDirectory: directoryName);
            }
            catch (UnauthorizedAccessException)
            {
                DynamicDialog dialog = DynamicDialogFactory.GetFor_ConsentDialog();
                await dialog.ShowAsync();
            }
            catch (ArgumentException)
            {
                if (new DirectoryInfo(e.ItemPath).Root.ToString().Contains(@"C:\"))
                {
                    AppInstance.NavigateWithArguments(FolderSettings.GetLayoutType(e.ItemPath), new NavigationArguments()
                    {
                        NavPathParam = e.ItemPath
                    });
                }
                else
                {
                    foreach (DriveItem drive in Enumerable.Concat(App.DrivesManager.Drives, AppSettings.CloudDrivesManager.Drives))
                    {
                        if (drive.Path.ToString() == new DirectoryInfo(e.ItemPath).Root.ToString())
                        {
                            AppInstance.NavigateWithArguments(FolderSettings.GetLayoutType(e.ItemPath), new NavigationArguments()
                            {
                                NavPathParam = e.ItemPath
                            });
                            return;
                        }
                    }
                }
            }
            catch (COMException)
            {
                await DialogDisplayHelper.ShowDialogAsync(
                    "DriveUnpluggedDialog/Title".GetLocalized(),
                    "DriveUnpluggedDialog/Text".GetLocalized());
            }
        }

        private void RecentFilesWidget_RecentFilesOpenLocationInvoked(object sender, UserControls.PathNavigationEventArgs e)
        {
            AppInstance.NavigateWithArguments(FolderSettings.GetLayoutType(e.ItemPath), new NavigationArguments()
            {
                NavPathParam = e.ItemPath
            });
        }

        private void LibraryWidget_LibraryCardInvoked(object sender, LibraryCardInvokedEventArgs e)
        {
            AppInstance.NavigateWithArguments(FolderSettings.GetLayoutType(e.Path), new NavigationArguments()
            {
                NavPathParam = e.Path
            });
            AppInstance.InstanceViewModel.IsPageTypeNotHome = true;     // show controls that were hidden on the home page
        }

        private void LibraryWidget_LibraryCardNewPaneInvoked(object sender, LibraryCardInvokedEventArgs e)
        {
            AppInstance.PaneHolder?.OpenPathInNewPane(e.Path);
        }

        private async void LibraryWidget_LibraryCardPropertiesInvoked(object sender, LibraryCardEventArgs e)
        {
            await FilePropertiesHelpers.OpenPropertiesWindowAsync(new LibraryItem(e.Library), AppInstance);
        }

        private async void LibraryWidget_LibraryCardDeleteInvoked(object sender, LibraryCardEventArgs e)
        {
            await AppInstance.FilesystemHelpers.DeleteItemAsync(new StorageFileWithPath(null, e.Library.Path), false, false, false);
        }

        private void DrivesWidget_DrivesWidgetNewPaneInvoked(object sender, DrivesWidget.DrivesWidgetInvokedEventArgs e)
        {
            AppInstance.PaneHolder?.OpenPathInNewPane(e.Path);
        }

        private void DrivesWidget_DrivesWidgetInvoked(object sender, DrivesWidget.DrivesWidgetInvokedEventArgs e)
        {
            AppInstance.NavigateWithArguments(FolderSettings.GetLayoutType(e.Path), new NavigationArguments()
            {
                NavPathParam = e.Path
            });
            AppInstance.InstanceViewModel.IsPageTypeNotHome = true;     // show controls that were hidden on the home page
        }

        protected override async void OnNavigatedTo(NavigationEventArgs eventArgs)
        {
            base.OnNavigatedTo(eventArgs);
            var parameters = eventArgs.Parameter as NavigationArguments;
            AppInstance = parameters.AssociatedTabInstance;
            AppInstance.InstanceViewModel.IsPageTypeNotHome = false;
            AppInstance.InstanceViewModel.IsPageTypeSearchResults = false;
            AppInstance.InstanceViewModel.IsPageTypeMtpDevice = false;
            AppInstance.InstanceViewModel.IsPageTypeRecycleBin = false;
            AppInstance.InstanceViewModel.IsPageTypeCloudDrive = false;
            AppInstance.NavigationToolbar.CanRefresh = false;
            AppInstance.NavigationToolbar.CanGoBack = AppInstance.CanNavigateBackward;
            AppInstance.NavigationToolbar.CanGoForward = AppInstance.CanNavigateForward;
            AppInstance.NavigationToolbar.CanNavigateToParent = false;

            // Set path of working directory empty
            await AppInstance.FilesystemViewModel.SetWorkingDirectoryAsync("Home");

            // Clear the path UI and replace with Favorites
            AppInstance.NavigationToolbar.PathComponents.Clear();
            string componentLabel = parameters.NavPathParam;
            string tag = parameters.NavPathParam;
            PathBoxItem item = new PathBoxItem()
            {
                Title = componentLabel,
                Path = tag,
            };
            AppInstance.NavigationToolbar.PathComponents.Add(item);
        }

        #region IDisposable

        // TODO: This Dispose() is never called, please implement the functionality to call this function.
        //       This IDisposable.Dispose() needs to be called to unhook events in BundlesWidget to avoid memory leaks.
        public void Dispose()
        {
            Widgets?.Dispose();
        }

        #endregion IDisposable
    }
}