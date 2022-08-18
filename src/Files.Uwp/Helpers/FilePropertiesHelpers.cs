using Files.Uwp.Dialogs;
using Files.Uwp.Views;
using Files.Uwp.Extensions;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation.Metadata;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.Graphics;
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
                    Frame frame = new Frame();
                    frame.RequestedTheme = ThemeHelper.RootTheme;
                    frame.Navigate(typeof(Properties), new PropertiesPageNavigationArguments()
                    {
                        Item = item,
                        AppInstanceArgument = associatedInstance
                    }, new SuppressNavigationTransitionInfo());

                    Window w = new Window();
                    w.Content = frame;
                    var appWindow = App.GetAppWindow(w);
                    (frame.Content as Properties).appWindow = appWindow;

                    appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
                    appWindow.Title = "PropertiesTitle".GetLocalizedResource();
                    appWindow.Resize(new SizeInt32(460, 550));
                    appWindow.Show();
                    if (true) // WINUI3: move window to cursor position
                    {
                        /*
                        // Set window size again here as sometimes it's not resized in the page Loaded event
                        appWindow.Resize(new SizeInt32(460, 550));

                        DisplayRegion displayRegion = ApplicationView.GetForCurrentView().GetDisplayRegions()[0];
                        Point pointerPosition = CoreWindow.GetForCurrentThread().PointerPosition;
                        appWindow.RequestMoveRelativeToDisplayRegion(displayRegion,
                            new Point(pointerPosition.X - displayRegion.WorkAreaOffset.X, pointerPosition.Y - displayRegion.WorkAreaOffset.Y));
                        */
                    }
                }
                else
                {
                    //WINUI3
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