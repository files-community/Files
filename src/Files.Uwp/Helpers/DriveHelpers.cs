using Files.Uwp.Interacts;
using Files.Uwp.Extensions;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.UI.Notifications;
using CommunityToolkit.WinUI.Notifications;

namespace Files.Uwp.Helpers
{
    public static class DriveHelpers
    {
        public static async Task EjectDeviceAsync(string path)
        {
            var removableDevice = new RemovableDevice(path);
            bool result = await removableDevice.EjectAsync();
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
    }
}