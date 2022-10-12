using Files.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Files.App.Shell;
using System.Diagnostics;
using CommunityToolkit.WinUI.Notifications;
using Files.App.Extensions;
using CommunityToolkit.WinUI;
using Files.App.Dialogs;
using System.Runtime.Versioning;
using Microsoft.Windows.AppNotifications;
using Windows.System;

namespace Files.App.Helpers
{
    public static class UIHelpers
    {
        /// <summary>
        /// Displays a dialog prompting the user to grant access to the filesystem
        /// </summary>
        /// <returns></returns>
        public static Task RequestFilesystemConsentAsync()
        {
            if (App.DrivesManager?.ShowUserConsentOnInit ?? false)
            {
                App.DrivesManager.ShowUserConsentOnInit = false;
                return App.Window.DispatcherQueue.EnqueueAsync(async () =>
                {
                    DynamicDialog dialog = DynamicDialogFactory.GetFor_ConsentDialog();
                    await SetContentDialogRoot(dialog).ShowAsync(ContentDialogPlacement.Popup);
                });
            }
            else
            {
                return Task.CompletedTask;
            }
        }

        /// <summary>
        /// Set the XamlRoot of the ContentDialog to the XamlRoot of the main window
        /// </summary>
        /// <param name="contentDialog"></param>
        /// <returns></returns>
        public static ContentDialog SetContentDialogRoot(ContentDialog contentDialog)
        {
            if (Windows.Foundation.Metadata.ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
            {
                contentDialog.XamlRoot = App.Window.Content.XamlRoot;
            }
            return contentDialog;
        }

        public static LauncherOptions InitializeWithWindow(LauncherOptions obj)
        {
            WinRT.Interop.InitializeWithWindow.Initialize(obj, App.WindowHandle);
            return obj;
        }

        /// <summary>
        /// Displays a toast or dialog to indicate the result of
        /// a device ejection operation.
        /// </summary>
        /// <param name="result">Only true implies a successful device ejection</param>
        /// <returns></returns>
        [SupportedOSPlatform("windows10.0.18362.0")]
        public static Task ShowDeviceEjectResultAsync(bool result)
        {
            if (result)
            {
                ToastContentBuilder toastContent = new ToastContentBuilder()
                    .AddText("EjectNotificationHeader".GetLocalizedResource())
                    .AddText("EjectNotificationBody".GetLocalizedResource())
                    .SetProtocolActivation(new Uri("files-uwp://"))
                    .AddAttributionText("SettingsAboutAppName".GetLocalizedResource());

                // Create the app notification
                var appNotif = new AppNotification(toastContent.GetXml().DocumentElement.GetXml());

                // Send the notification
                AppNotificationManager.Default.Show(appNotif);

                return Task.CompletedTask;
            }
            else
            {
                Debug.WriteLine("Can't eject device");

                return DialogDisplayHelper.ShowDialogAsync(
                    "EjectNotificationErrorDialogHeader".GetLocalizedResource(),
                    "EjectNotificationErrorDialogBody".GetLocalizedResource());
            }
        }

        public static async Task<ContentDialogResult> TryShowAsync(this ContentDialog dialog)
        {
            try
            {
                return await SetContentDialogRoot(dialog).ShowAsync();
            }
            catch // A content dialog is already open
            {
                return ContentDialogResult.None;
            }
        }

        public static void CloseAllDialogs()
        {
            var openedDialogs = VisualTreeHelper.GetOpenPopups(App.Window);

            foreach (var item in openedDialogs)
            {
                if (item.Child is ContentDialog dialog)
                {
                    dialog.Hide();
                }
            }
        }

        private static IEnumerable<IconFileInfo> IconResources = UIHelpers.LoadSidebarIconResources();

        public static IconFileInfo GetIconResourceInfo(int index)
        {
            var icons = UIHelpers.IconResources;
            if (icons != null)
            {
                return icons.FirstOrDefault(x => x.Index == index);
            }
            return null;
        }

        public static async Task<BitmapImage> GetIconResource(int index)
        {
            var iconInfo = GetIconResourceInfo(index);
            if (iconInfo != null)
            {
                return await iconInfo.IconData.ToBitmapAsync();
            }
            return null;
        }

        private static IEnumerable<IconFileInfo> LoadSidebarIconResources()
        {
            string imageres = Path.Combine(CommonPaths.SystemRootPath, "System32", "imageres.dll");
            var imageResList = Win32API.ExtractSelectedIconsFromDLL(imageres, new List<int>() {
                    Constants.ImageRes.RecycleBin,
                    Constants.ImageRes.NetworkDrives,
                    Constants.ImageRes.Libraries,
                    Constants.ImageRes.ThisPC,
                    Constants.ImageRes.CloudDrives,
                    Constants.ImageRes.Folder
                }, 32);

            return imageResList;
        }
    }
}