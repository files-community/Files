using Files.Dialogs;
using Files.Filesystem;
using Files.Helpers;
using Files.UserControls.Widgets;
using Files.ViewModels;
using Files.ViewModels.Bundles;
using Microsoft.Toolkit.Uwp;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Windows.ApplicationModel.AppService;
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

        public YourHome()
        {
            InitializeComponent();
            this.Loaded += YourHome_Loaded;
        }

        private void YourHome_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if (DrivesWidget != null)
            {
                DrivesWidget.DrivesWidgetInvoked -= DrivesWidget_DrivesWidgetInvoked;
                DrivesWidget.DrivesWidgetNewPaneInvoked -= DrivesWidget_DrivesWidgetNewPaneInvoked;
                DrivesWidget.DrivesWidgetInvoked += DrivesWidget_DrivesWidgetInvoked;
                DrivesWidget.DrivesWidgetNewPaneInvoked += DrivesWidget_DrivesWidgetNewPaneInvoked;
            }
            if (LibraryWidget != null)
            {
                LibraryWidget.LibraryCardInvoked -= LibraryLocationCardsWidget_LibraryCardInvoked;
                LibraryWidget.LibraryCardInvoked += LibraryLocationCardsWidget_LibraryCardInvoked;
            }
            if (RecentFilesWidget != null)
            {
                RecentFilesWidget.RecentFilesOpenLocationInvoked -= RecentFilesWidget_RecentFilesOpenLocationInvoked;
                RecentFilesWidget.RecentFileInvoked -= RecentFilesWidget_RecentFileInvoked;
                RecentFilesWidget.RecentFilesOpenLocationInvoked += RecentFilesWidget_RecentFilesOpenLocationInvoked;
                RecentFilesWidget.RecentFileInvoked += RecentFilesWidget_RecentFileInvoked;
            }
            if (BundlesWidget != null)
            {
                (BundlesWidget?.DataContext as BundlesViewModel)?.Initialize(AppInstance);
                (BundlesWidget?.DataContext as BundlesViewModel)?.Load();
            }
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

        private void LibraryLocationCardsWidget_LibraryCardInvoked(object sender, LibraryCardInvokedEventArgs e)
        {
            AppInstance.NavigateWithArguments(FolderSettings.GetLayoutType(e.Path), new NavigationArguments()
            {
                NavPathParam = e.Path
            });
            AppInstance.InstanceViewModel.IsPageTypeNotHome = true;     // show controls that were hidden on the home page
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
            BundlesWidget?.Dispose();
        }

        #endregion IDisposable
    }
}