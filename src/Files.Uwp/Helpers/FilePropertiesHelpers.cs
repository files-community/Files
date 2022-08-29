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
using Microsoft.UI;
using static Files.Uwp.Views.Properties;
using Microsoft.UI.Windowing;

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
                    var frame = new Frame();
                    frame.RequestedTheme = ThemeHelper.RootTheme;
                    frame.Navigate(typeof(Properties), new PropertiesPageNavigationArguments()
                    {
                        Item = item,
                        AppInstanceArgument = associatedInstance
                    }, new SuppressNavigationTransitionInfo());

                    // Initialize window
                    var propertiesWindow = new WinUIEx.WindowEx();
                    var appWindow = propertiesWindow.AppWindow;

                    // Set content
                    propertiesWindow.Content = frame;
                    if (frame.Content is Properties properties)
                        properties.appWindow = appWindow;

                    // Set min size
                    propertiesWindow.MinWidth = 460;
                    propertiesWindow.MinHeight = 550;

                    // Set backdrop
                    propertiesWindow.Backdrop = new WinUIEx.MicaSystemBackdrop() { DarkTintOpacity = 0.8 };

                    if (AppWindowTitleBar.IsCustomizationSupported())
                    {
                        appWindow.TitleBar.ExtendsContentIntoTitleBar = true;

                        // Set window buttons background to transparent
                        appWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
                        appWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
                    }
                    else
                    {
                        propertiesWindow.ExtendsContentIntoTitleBar = true;
                    }

                    appWindow.Title = "PropertiesTitle".GetLocalizedResource();
                    appWindow.Resize(new SizeInt32(460, 550));
                    appWindow.Show();

                    if (true) // WINUI3: move window to cursor position, todo better
                    {
                        UWPToWinAppSDKUpgradeHelpers.InteropHelpers.GetCursorPos(out var pointerPosition);
                        appWindow.Move(new PointInt32(pointerPosition.X, pointerPosition.Y));
                    }
                }
                else
                {
                    //WINUI3: no CoreApplicationView
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