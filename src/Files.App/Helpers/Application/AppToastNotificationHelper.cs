using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Files.App.Helpers.Application
{
	internal static class AppToastNotificationHelper
	{
		public static void ShowUnhandledExceptionToast()
		{
			var toastContent = new AppNotificationBuilder()
					.AddText(Strings.ExceptionNotificationHeader.GetLocalizedResource())
					.AddText(Strings.ExceptionNotificationBody.GetLocalizedResource())
					.SetAppLogoOverride(new Uri("ms-appx:///Assets/error.png"))
					.AddButton(new AppNotificationButton(Strings.ExceptionNotificationReportButton.GetLocalizedResource())
						.SetInvokeUri(new Uri(Constants.ExternalUrl.BugReportUrl)))
					.BuildNotification();
			AppNotificationManager.Default.Show(toastContent);
		}

		public static void ShowBackgroundRunningToast()
		{
			var toastContent = new AppNotificationBuilder()
				.AddText(Strings.BackgroundRunningNotificationHeader.GetLocalizedResource())
				.AddText(Strings.BackgroundRunningNotificationBody.GetLocalizedResource())
				.BuildNotification();
			AppNotificationManager.Default.Show(toastContent);
		}

		public static void ShowDriveEjectToast()
		{
			var toastContent = new AppNotificationBuilder()
				.AddText(Strings.EjectNotificationHeader.GetLocalizedResource())
				.AddText(Strings.EjectNotificationBody.GetLocalizedResource())
				.SetAttributionText("SettingsAboutAppName".GetLocalizedResource())
				.BuildNotification();
			AppNotificationManager.Default.Show(toastContent);
		}
	}
}
