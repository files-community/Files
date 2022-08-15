using Files.Uwp.Dialogs;
using Files.Uwp.Views;
using Microsoft.Toolkit.Uwp;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.WindowManagement;
using Windows.UI.WindowManagement.Preview;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media.Animation;
using static Files.Uwp.Views.Properties;

namespace Files.Uwp.Helpers
{
    public static class FilePropertiesHelpers
    {
        public static async void ShowProperties(IShellPage associatedInstance)
        {
            if (associatedInstance.SlimContentPage.IsItemSelected)
            {
                if (associatedInstance.SlimContentPage.SelectedItems.Count > 1)
                {
                    await OpenPropertiesWindowAsync(associatedInstance.SlimContentPage.SelectedItems, associatedInstance);
                }
                else
                {
                    await OpenPropertiesWindowAsync(associatedInstance.SlimContentPage.SelectedItem, associatedInstance);
                }
            }
            else
            {
                if (!System.IO.Path.GetPathRoot(associatedInstance.FilesystemViewModel.CurrentFolder.ItemPath)
                    .Equals(associatedInstance.FilesystemViewModel.CurrentFolder.ItemPath, StringComparison.OrdinalIgnoreCase))
                {
                    await OpenPropertiesWindowAsync(associatedInstance.FilesystemViewModel.CurrentFolder, associatedInstance);
                }
                else
                {
                    await OpenPropertiesWindowAsync(App.DrivesManager.Drives
                        .SingleOrDefault(x => x.Path.Equals(associatedInstance.FilesystemViewModel.CurrentFolder.ItemPath)), associatedInstance);
                }
            }
        }

        public static async Task OpenPropertiesWindowAsync(object item, IShellPage associatedInstance)
        {
            if (item == null)
            {
                return;
            }

            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
            {
                if (WindowDecorationsHelper.IsWindowDecorationsAllowed)
                {

                /*
                   TODO UA315_A Use Microsoft.UI.Windowing.AppWindow for window Management instead of ApplicationView/CoreWindow or Microsoft.UI.Windowing.AppWindow APIs
                   Read: https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/guides/windowing
                */
                Microsoft.UI.Windowing.AppWindow appWindow =                 /*
                   TODO UA315_A Use Microsoft.UI.Windowing.AppWindow for window Management instead of ApplicationView/CoreWindow or Microsoft.UI.Windowing.AppWindow APIs
                   Read: https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/guides/windowing
                */
                Microsoft.UI.Windowing.AppWindow.Create();

                    Frame frame = new Frame();
                    frame.RequestedTheme = ThemeHelper.RootTheme;
                    frame.Navigate(typeof(Properties), new PropertiesPageNavigationArguments()
                    {
                        Item = item,
                        AppInstanceArgument = associatedInstance
                    }, new SuppressNavigationTransitionInfo());
                    ElementCompositionPreview.SetAppWindowContent(appWindow, frame);
                    (frame.Content as Properties).appWindow = appWindow;

                    appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
                    appWindow.Title = "PropertiesTitle".GetLocalized();
                    appWindow.PersistedStateId = "Properties";
                    WindowManagementPreview.SetPreferredMinSize(appWindow, new Size(460, 550));

                    bool windowShown = await appWindow.TryShowAsync();
                    if (windowShown)
                    {
                        // Set window size again here as sometimes it's not resized in the page Loaded event
                        appWindow.RequestSize(new Size(460, 550));

                        DisplayRegion displayRegion = 
                /*
                   TODO UA315_A Use Microsoft.UI.Windowing.AppWindow for window Management instead of ApplicationView/CoreWindow or Microsoft.UI.Windowing.AppWindow APIs
                   Read: https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/guides/windowing
                */
                ApplicationView.GetForCurrentView().GetDisplayRegions()[0];
                        Point pointerPosition = 
                    /* 
                        TODO UA315_B
                        Use Microsoft.UI.Windowing.AppWindow.Create instead of GetForCurrentThread.
                        Read: https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/guides/windowing
                    */
                    CoreWindow.GetForCurrentThread().PointerPosition;
                        appWindow.RequestMoveRelativeToDisplayRegion(displayRegion,
                            new Point(pointerPosition.X - displayRegion.WorkAreaOffset.X, pointerPosition.Y - displayRegion.WorkAreaOffset.Y));
                    }
                }
                else
                {
                    CoreApplicationView newWindow = CoreApplication.CreateNewView();

                /*
                   TODO UA315_A Use Microsoft.UI.Windowing.AppWindow for window Management instead of ApplicationView/CoreWindow or Microsoft.UI.Windowing.AppWindow APIs
                   Read: https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/guides/windowing
                */
                ApplicationView newView = null;

                    await /*
                TODO UA306_A2: UWP CoreDispatcher : Windows.UI.Core.CoreDispatcher is no longer supported. Use DispatcherQueue instead. Read: https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/guides/threading
            */newWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                    {
                        Frame frame = new Frame();
                        frame.RequestedTheme = ThemeHelper.RootTheme;
                        frame.Navigate(typeof(Properties), new PropertiesPageNavigationArguments()
                        {
                            Item = item,
                            AppInstanceArgument = associatedInstance
                        }, new SuppressNavigationTransitionInfo());
                        App.Window.Content = frame;
                        App.Window.Activate();

                        newView = 
                /*
                   TODO UA315_A Use Microsoft.UI.Windowing.AppWindow for window Management instead of ApplicationView/CoreWindow or Microsoft.UI.Windowing.AppWindow APIs
                   Read: https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/guides/windowing
                */
                ApplicationView.GetForCurrentView();
                        newWindow.TitleBar.ExtendViewIntoTitleBar = true;
                        newView.Title = "PropertiesTitle".GetLocalized();
                        newView.PersistedStateId = "Properties";
                        newView.SetPreferredMinSize(new Size(460, 550));

                        bool viewShown = await ApplicationViewSwitcher.TryShowAsStandaloneAsync(newView.Id);
                        if (viewShown && newView != null)
                        {
                            // Set window size again here as sometimes it's not resized in the page Loaded event
                            newView.TryResizeView(new Size(460, 550));
                        }
                    });
                }
            }
            else
            {
                var propertiesDialog = new PropertiesDialog();
                propertiesDialog.propertiesFrame.Tag = propertiesDialog;
                propertiesDialog.propertiesFrame.Navigate(typeof(Properties), new PropertiesPageNavigationArguments()
                {
                    Item = item,
                    AppInstanceArgument = associatedInstance
                }, new SuppressNavigationTransitionInfo());
                await propertiesDialog.ShowAsync(ContentDialogPlacement.Popup);
            }
        }
    }
}