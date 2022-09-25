using Files.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Files.App.Shell;
using System.Diagnostics;
using CommunityToolkit.WinUI.Notifications;
using Windows.UI.Notifications;
using Files.App.Extensions;

namespace Files.App.Helpers
{
    public static class UIHelpers
    {
        /// <summary>
        /// Displays a toast or dialog to indicate the result of
        /// a device ejection operation.
        /// </summary>
        /// <param name="result">Only true implies a successful device ejection</param>
        /// <returns></returns>
        public static async Task ShowDeviceEjectResultAsync(bool result)
        {
            if (result)
            {
                Debug.WriteLine("Device successfully ejected");

                var toastContent = new ToastContent()
                {
                    Visual = new ToastVisual()
                    {
                        BindingGeneric = new ToastBindingGeneric()
                        {
                            Children =
                            {
                                new AdaptiveText()
                                {
                                    Text = "EjectNotificationHeader".GetLocalizedResource()
                                },
                                new AdaptiveText()
                                {
                                    Text = "EjectNotificationBody".GetLocalizedResource()
                                }
                            },
                            Attribution = new ToastGenericAttributionText()
                            {
                                Text = "SettingsAboutAppName".GetLocalizedResource()
                            }
                        }
                    },
                    ActivationType = ToastActivationType.Protocol
                };

                // Create the toast notification
                var toastNotif = new ToastNotification(toastContent.GetXml());

                // And send the notification
                ToastNotificationManager.CreateToastNotifier().Show(toastNotif);
            }
            else
            {
                Debug.WriteLine("Can't eject device");

                await DialogDisplayHelper.ShowDialogAsync(
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

        // WINUI3
        private static ContentDialog SetContentDialogRoot(ContentDialog contentDialog)
        {
            if (Windows.Foundation.Metadata.ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
            {
                contentDialog.XamlRoot = App.Window.Content.XamlRoot;
            }
            return contentDialog;
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
                    Constants.ImageRes.EmptyRecycleBin,
                    Constants.ImageRes.NetworkDrives,
                    Constants.ImageRes.Libraries,
                    Constants.ImageRes.ThisPC,
                    Constants.ImageRes.CloudDrives,
                    Constants.ImageRes.Folder
                }, 32);

            return imageResList;
        }

        public static async Task<BitmapImage> RetrieveAdaptedRecycleBinIcon()
        {
            BitmapImage icon;
            if (new RecycleBinHelpers().RecycleBinHasItems())
            {
                icon = await UIHelpers.GetIconResource(Constants.ImageRes.RecycleBin);
            }
            else
            {
                icon = await UIHelpers.GetIconResource(Constants.ImageRes.EmptyRecycleBin);
            }

            return icon;
        }
    }
}