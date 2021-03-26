using Files.Interacts;
using Microsoft.Toolkit.Uwp;
using Microsoft.Toolkit.Uwp.Notifications;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.UI.Notifications;

namespace Files.Helpers
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
                                    Text = "EjectNotificationHeader".GetLocalized()
                                },
                                new AdaptiveText()
                                {
                                    Text = "EjectNotificationBody".GetLocalized()
                                }
                            },
                            Attribution = new ToastGenericAttributionText()
                            {
                                Text = "SettingsAboutAppName".GetLocalized()
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
                    "EjectNotificationErrorDialogHeader".GetLocalized(),
                    "EjectNotificationErrorDialogBody".GetLocalized());
            }
        }

        public static string GetDriveTypeIcon(System.IO.DriveInfo drive)
        {
            string type;

            switch (drive.DriveType)
            {
                case System.IO.DriveType.CDRom:
                    type = "\xE958";
                    break;

                case System.IO.DriveType.Fixed:
                    type = "\xEDA2";
                    break;

                case System.IO.DriveType.Network:
                    type = "\xE8CE";
                    break;

                case System.IO.DriveType.NoRootDirectory:
                    type = "\xED25";
                    break;

                case System.IO.DriveType.Ram:
                    type = "\xE950";
                    break;

                case System.IO.DriveType.Removable:
                    type = "\xE88E";
                    break;

                case System.IO.DriveType.Unknown:
                    if (PathNormalization.NormalizePath(drive.Name) != PathNormalization.NormalizePath("A:") && PathNormalization.NormalizePath(drive.Name) != PathNormalization.NormalizePath("B:"))
                    {
                        type = "\xEDA2";
                    }
                    else
                    {
                        type = "\xE74E"; // Floppy icon
                    }
                    break;

                default:
                    type = "\xEDA2"; // Drive icon
                    break;
            }

            return type;
        }
    }
}