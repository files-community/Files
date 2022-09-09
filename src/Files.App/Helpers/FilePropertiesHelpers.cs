using Files.App.Dialogs;
using Files.App.Extensions;
using Files.App.Views;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Foundation.Metadata;
using Windows.Graphics;
using static Files.App.Views.Properties;

#nullable enable

namespace Files.App.Helpers
{
    public static class FilePropertiesHelpers
    {
        public static async void ShowProperties(IShellPage associatedInstance)
        {
            if (associatedInstance.SlimContentPage.IsItemSelected)
            {
                if (associatedInstance.SlimContentPage.SelectedItems.Count > 1)
                    await OpenPropertiesWindowAsync(associatedInstance.SlimContentPage.SelectedItems, associatedInstance);
                else
                    await OpenPropertiesWindowAsync(associatedInstance.SlimContentPage.SelectedItem, associatedInstance);
            }
            else
            {
                var path = System.IO.Path.GetPathRoot(associatedInstance.FilesystemViewModel.CurrentFolder.ItemPath);
                if (path is not null && path.Equals(associatedInstance.FilesystemViewModel.CurrentFolder.ItemPath, StringComparison.OrdinalIgnoreCase))
                    await OpenPropertiesWindowAsync(associatedInstance.FilesystemViewModel.CurrentFolder, associatedInstance);
                else
                    await OpenPropertiesWindowAsync(App.DrivesManager.Drives
                        .Single(x => x.Path.Equals(associatedInstance.FilesystemViewModel.CurrentFolder.ItemPath)), associatedInstance);
            }
        }

        public static async Task OpenPropertiesWindowAsync(object item, IShellPage associatedInstance)
        {
            if (item == null)
                return;

            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
            {
                var frame = new Frame();
                frame.RequestedTheme = ThemeHelper.RootTheme;
                frame.Navigate(typeof(Properties), new PropertiesPageNavigationArguments()
                {
                    Item = item,
                    AppInstanceArgument = associatedInstance
                }, new SuppressNavigationTransitionInfo());

                // Initialize window
                var propertiesWindow = new WinUIEx.WindowEx()
                {
                    IsMaximizable = false,
                    IsMinimizable = false
                };
                var appWindow = propertiesWindow.AppWindow;

                // Set icon
                appWindow.SetIcon(Path.Combine(Package.Current.InstalledLocation.Path, "Assets/AppTiles/Dev/Logo.ico"));

                // Set content
                propertiesWindow.Content = frame;
                if (frame.Content is Properties properties)
                    properties.appWindow = appWindow;

                // Set min size
                propertiesWindow.MinWidth = 460;
                propertiesWindow.MinHeight = 550;

                // Set backdrop
                propertiesWindow.Backdrop = new WinUIEx.MicaSystemBackdrop() { DarkTintOpacity = 0.8 };

                appWindow.TitleBar.ExtendsContentIntoTitleBar = true;

                // Set window buttons background to transparent
                appWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
                appWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

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
