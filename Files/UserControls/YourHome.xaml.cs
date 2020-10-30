using Files.Dialogs;
using Files.Filesystem;
using Files.Helpers;
using Files.Interacts;
using Files.View_Models;
using Files.Views.Pages;
using System;
using System.IO;
using System.Runtime.InteropServices;
using Windows.ApplicationModel.AppService;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Files
{
    public sealed partial class YourHome : Page
    {
        public SettingsViewModel AppSettings => App.AppSettings;
        public IShellPage AppInstance = null;
        public AppServiceConnection Connection = null;
        public YourHome()
        {
            InitializeComponent();
            LibraryLocationCardsWidget.LibraryCardInvoked += LibraryLocationCardsWidget_LibraryCardInvoked;
            RecentFilesWidget.RecentFilesOpenLocationInvoked += RecentFilesWidget_RecentFilesOpenLocationInvoked;
            RecentFilesWidget.RecentFileInvoked += RecentFilesWidget_RecentFileInvoked;
        }

        private async void RecentFilesWidget_RecentFileInvoked(object sender, UserControls.PathNavigationEventArgs e)
        {
            try
            {
                var directoryName = Path.GetDirectoryName(e.ItemPath);
                await AppInstance.InteractionOperations.InvokeWin32Component(e.ItemPath, workingDir: directoryName);
            }
            catch (UnauthorizedAccessException)
            {
                var consentDialog = new ConsentDialog();
                await consentDialog.ShowAsync();
            }
            catch (ArgumentException)
            {
                if (new DirectoryInfo(e.ItemPath).Root.ToString().Contains(@"C:\"))
                {
                    AppInstance.ContentFrame.Navigate(AppSettings.GetLayoutType(), new NavigationArguments() { AssociatedTabInstance = AppInstance, NavPathParam = e.ItemPath, ServiceConnection = Connection });
                }
                else
                {
                    foreach (DriveItem drive in AppSettings.DrivesManager.Drives)
                    {
                        if (drive.Path.ToString() == new DirectoryInfo(e.ItemPath).Root.ToString())
                        {
                            AppInstance.ContentFrame.Navigate(AppSettings.GetLayoutType(), new NavigationArguments() { AssociatedTabInstance = AppInstance, NavPathParam = e.ItemPath, ServiceConnection = Connection });
                            return;
                        }
                    }
                }
            }
            catch (COMException)
            {
                await DialogDisplayHelper.ShowDialog(
                    ResourceController.GetTranslation("DriveUnpluggedDialog/Title"),
                    ResourceController.GetTranslation("DriveUnpluggedDialog/Text"));
            }
        }

        private void RecentFilesWidget_RecentFilesOpenLocationInvoked(object sender, UserControls.PathNavigationEventArgs e)
        {
            AppInstance.ContentFrame.Navigate(e.LayoutType, new NavigationArguments() { NavPathParam = e.ItemPath, AssociatedTabInstance = AppInstance, ServiceConnection = Connection });
        }

        private void LibraryLocationCardsWidget_LibraryCardInvoked(object sender, LibraryCardInvokedEventArgs e)
        {
            AppInstance.ContentFrame.Navigate(e.LayoutType, new NavigationArguments() { NavPathParam = e.Path, AssociatedTabInstance = AppInstance, ServiceConnection = Connection });
            AppInstance.InstanceViewModel.IsPageTypeNotHome = true;     // show controls that were hidden on the home page        
        }

        protected override async void OnNavigatedTo(NavigationEventArgs eventArgs)
        {
            base.OnNavigatedTo(eventArgs);
            var parameters = eventArgs.Parameter as NavigationArguments;
            AppInstance = parameters.AssociatedTabInstance;
            Connection = parameters.ServiceConnection;
            AppInstance.InstanceViewModel.IsPageTypeNotHome = false;
            AppInstance.InstanceViewModel.IsPageTypeMtpDevice = false;
            AppInstance.InstanceViewModel.IsPageTypeRecycleBin = false;
            AppInstance.InstanceViewModel.IsPageTypeCloudDrive = false;
            App.MultitaskingControl?.SetSelectedTabInfo(parameters.NavPathParam, null);
            App.MultitaskingControl?.SelectionChanged();
            AppInstance.NavigationToolbar.CanRefresh = false;
            AppInstance.NavigationToolbar.CanGoBack = AppInstance.ContentFrame.CanGoBack;
            AppInstance.NavigationToolbar.CanGoForward = AppInstance.ContentFrame.CanGoForward;
            AppInstance.NavigationToolbar.CanNavigateToParent = false;

            // Set path of working directory empty
            await AppInstance.FilesystemViewModel.SetWorkingDirectory("Home");

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
    }
}