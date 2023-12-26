// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.WinUI.Notifications;
using Windows.UI.Notifications;

namespace Files.App.Helpers
{
	internal static class AppNotificationHelper
	{
		/// <summary>
		/// Displays a toast or dialog to indicate the result of
		/// a device ejection operation.
		/// </summary>
		public static async Task ShowDeviceEjectResultAsync(DriveType type, bool result)
		{
			if (type != DriveType.CDRom && result)
			{
				Debug.WriteLine("Device have successfully ejected.");

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
				var toastNotification = new ToastNotification(toastContent.GetXml());

				// And send the notification
				ToastNotificationManager.CreateToastNotifier().Show(toastNotification);
			}
			else if (!result)
			{
				Debug.WriteLine("Could not eject the device.");

				await ContentDialogHelper.ShowDialogAsync(
					"EjectNotificationErrorDialogHeader".GetLocalizedResource(),
					"EjectNotificationErrorDialogBody".GetLocalizedResource());
			}
		}

		public static void ShowApplicationCaughtUnhandledException()
		{
			var toastContent = new ToastContent()
			{
				Visual = new()
				{
					BindingGeneric = new ToastBindingGeneric()
					{
						Children =
						{
							new AdaptiveText()
							{
								Text = "ExceptionNotificationHeader".GetLocalizedResource()
							},
							new AdaptiveText()
							{
								Text = "ExceptionNotificationBody".GetLocalizedResource()
							}
						},
						AppLogoOverride = new()
						{
							Source = "ms-appx:///Assets/error.png"
						}
					}
				},
				Actions = new ToastActionsCustom()
				{
					Buttons =
					{
						new ToastButton("ExceptionNotificationReportButton".GetLocalizedResource(), Constants.GitHub.BugReportUrl)
						{
							ActivationType = ToastActivationType.Protocol
						}
					}
				},
				ActivationType = ToastActivationType.Protocol
			};

			// Create the toast notification
			var toastNotification = new ToastNotification(toastContent.GetXml());

			// And send the notification
			ToastNotificationManager.CreateToastNotifier().Show(toastNotification);
		}
	}
}
