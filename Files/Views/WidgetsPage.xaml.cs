using Files.DataModels.NavigationControlItems;
using Files.Dialogs;
using Files.Filesystem;
using Files.Helpers;
using Files.UserControls.Widgets;
using Files.ViewModels;
using Files.ViewModels.Pages;
using Microsoft.Toolkit.Uwp;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Files.Views
{
    public sealed partial class WidgetsPage : Page, IDisposable
    {
        private IShellPage AppInstance = null;
        public FolderSettingsViewModel FolderSettings => AppInstance?.InstanceViewModel.FolderSettings;

        private LibraryCards libraryCards;
        private DrivesWidget drivesWidget;
        private Bundles bundles;
        private RecentFiles recentFiles;

        public YourHomeViewModel ViewModel
        {
            get => (YourHomeViewModel)DataContext;
            set => DataContext = value;
        }

        public WidgetsPage()
        {
            InitializeComponent();

            ViewModel = new YourHomeViewModel(Widgets.ViewModel, AppInstance);
            ViewModel.YourHomeLoadedInvoked += ViewModel_YourHomeLoadedInvoked;
            Widgets.ViewModel.WidgetListRefreshRequestedInvoked += ViewModel_WidgetListRefreshRequestedInvoked;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.Dispose();

            base.OnNavigatedFrom(e);
        }

        public void RefreshWidgetList() => Widgets.ViewModel.RefreshWidgetList();

        private void ViewModel_WidgetListRefreshRequestedInvoked(object sender, EventArgs e)
        {
            ReloadWidgets();
        }

        private void ReloadWidgets()
        {
            libraryCards = WidgetsHelpers.TryGetWidget<LibraryCards>(Widgets.ViewModel, out bool shouldReloadLibraryCards, libraryCards);
            drivesWidget = WidgetsHelpers.TryGetWidget<DrivesWidget>(Widgets.ViewModel, out bool shouldReloadDrivesWidget, drivesWidget);
            bundles = WidgetsHelpers.TryGetWidget<Bundles>(Widgets.ViewModel, out bool shouldReloadBundles, bundles);
            recentFiles = WidgetsHelpers.TryGetWidget<RecentFiles>(Widgets.ViewModel, out bool shouldReloadRecentFiles, recentFiles);

            if (shouldReloadLibraryCards && libraryCards != null)
            {
                Widgets.ViewModel.InsertWidget(libraryCards, 0);
                libraryCards.LoadIconOverlay = AppInstance.FilesystemViewModel.LoadIconWithoutOverlayAsync;

                libraryCards.LibraryCardInvoked -= LibraryWidget_LibraryCardInvoked;
                libraryCards.LibraryCardNewPaneInvoked -= LibraryWidget_LibraryCardNewPaneInvoked;
                libraryCards.LibraryCardPropertiesInvoked -= LibraryWidget_LibraryCardPropertiesInvoked;
                libraryCards.LibraryCardDeleteInvoked -= LibraryWidget_LibraryCardDeleteInvoked;
                libraryCards.LibraryCardShowMultiPaneControlsInvoked -= LibraryCards_LibraryCardShowMultiPaneControlsInvoked;
                libraryCards.LibraryCardInvoked += LibraryWidget_LibraryCardInvoked;
                libraryCards.LibraryCardNewPaneInvoked += LibraryWidget_LibraryCardNewPaneInvoked;
                libraryCards.LibraryCardPropertiesInvoked += LibraryWidget_LibraryCardPropertiesInvoked;
                libraryCards.LibraryCardDeleteInvoked += LibraryWidget_LibraryCardDeleteInvoked;
                libraryCards.LibraryCardShowMultiPaneControlsInvoked += LibraryCards_LibraryCardShowMultiPaneControlsInvoked;
            }
            if (shouldReloadDrivesWidget && drivesWidget != null)
            {
                Widgets.ViewModel.InsertWidget(drivesWidget, 1);

                drivesWidget.AppInstance = AppInstance;
                drivesWidget.DrivesWidgetInvoked -= DrivesWidget_DrivesWidgetInvoked;
                drivesWidget.DrivesWidgetNewPaneInvoked -= DrivesWidget_DrivesWidgetNewPaneInvoked;
                drivesWidget.DrivesWidgetInvoked += DrivesWidget_DrivesWidgetInvoked;
                drivesWidget.DrivesWidgetNewPaneInvoked += DrivesWidget_DrivesWidgetNewPaneInvoked;
            }
            if (shouldReloadBundles && bundles != null)
            {
                Widgets.ViewModel.InsertWidget(bundles, 2);
                ViewModel.LoadBundlesCommand.Execute(bundles.ViewModel);
            }
            if (shouldReloadRecentFiles && recentFiles != null)
            {
                Widgets.ViewModel.InsertWidget(recentFiles, 3);

                recentFiles.RecentFilesOpenLocationInvoked -= RecentFilesWidget_RecentFilesOpenLocationInvoked;
                recentFiles.RecentFileInvoked -= RecentFilesWidget_RecentFileInvoked;
                recentFiles.RecentFilesOpenLocationInvoked += RecentFilesWidget_RecentFilesOpenLocationInvoked;
                recentFiles.RecentFileInvoked += RecentFilesWidget_RecentFileInvoked;
            }
        }

        private void ViewModel_YourHomeLoadedInvoked(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            // We must change the associatedInstance because only now it has loaded and not null
            ViewModel.ChangeAppInstance(AppInstance);
            ReloadWidgets();
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
                    foreach (DriveItem drive in Enumerable.Concat(App.DrivesManager.Drives, App.AppSettings.CloudDrivesManager.Drives))
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

            AppInstance.LoadPreviewPaneChanged();

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

        public void Dispose()
        {
            ViewModel.YourHomeLoadedInvoked -= ViewModel_YourHomeLoadedInvoked;
            Widgets.ViewModel.WidgetListRefreshRequestedInvoked -= ViewModel_WidgetListRefreshRequestedInvoked;
            ViewModel?.Dispose();
        }

        #endregion IDisposable
    }
}