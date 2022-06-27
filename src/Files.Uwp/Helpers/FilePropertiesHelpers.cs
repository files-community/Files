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
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Media.Animation;
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
                    AppWindow appWindow = await AppWindow.TryCreateAsync();

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

                        DisplayRegion displayRegion = ApplicationView.GetForCurrentView().GetDisplayRegions()[0];
                        Point pointerPosition = CoreWindow.GetForCurrentThread().PointerPosition;
                        appWindow.RequestMoveRelativeToDisplayRegion(displayRegion,
                            new Point(pointerPosition.X - displayRegion.WorkAreaOffset.X, pointerPosition.Y - displayRegion.WorkAreaOffset.Y));
                    }
                }
                else
                {
                    CoreApplicationView newWindow = CoreApplication.CreateNewView();
                    ApplicationView newView = null;

                    await newWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                    {
                        Frame frame = new Frame();
                        frame.RequestedTheme = ThemeHelper.RootTheme;
                        frame.Navigate(typeof(Properties), new PropertiesPageNavigationArguments()
                        {
                            Item = item,
                            AppInstanceArgument = associatedInstance
                        }, new SuppressNavigationTransitionInfo());
                        Window.Current.Content = frame;
                        Window.Current.Activate();

                        newView = ApplicationView.GetForCurrentView();
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