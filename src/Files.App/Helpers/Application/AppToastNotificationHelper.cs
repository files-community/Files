using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;

namespace Files.App.Helpers.Application
{
	internal static class AppToastNotificationHelper
	{
		public static void ShowUnhandledExceptionToast()
		{
			// App notifications require the registration that only packaged installs get
			if (!AppRuntimeHelper.IsPackaged)
				return;

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
			if (!AppRuntimeHelper.IsPackaged)
				return;

			var toastContent = new AppNotificationBuilder()
				.AddText(Strings.BackgroundRunningNotificationHeader.GetLocalizedResource())
				.AddText(Strings.BackgroundRunningNotificationBody.GetLocalizedResource())
				.BuildNotification();
			AppNotificationManager.Default.Show(toastContent);
		}

		public static void ShowDriveEjectToast()
		{
			if (!AppRuntimeHelper.IsPackaged)
				return;

			var toastContent = new AppNotificationBuilder()
				.AddText(Strings.EjectNotificationHeader.GetLocalizedResource())
				.AddText(Strings.EjectNotificationBody.GetLocalizedResource())
				.SetAttributionText("SettingsAboutAppName".GetLocalizedResource())
				.BuildNotification();
			AppNotificationManager.Default.Show(toastContent);
		}
	}
}
