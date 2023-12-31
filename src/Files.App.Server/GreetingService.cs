using Windows.UI.Notifications;

namespace Files.App.Server;

public sealed class GreetingService
{
	public static void ShowGreet()
	{
		var doc = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastImageAndText02);
		doc.SelectSingleNode("""//text[@id="1"]""").InnerText = "Welcome to Files!";
		doc.SelectSingleNode("""//text[@id="2"]""").InnerText = "This is a toast notification sent from Files.App.Server";
		ToastNotificationManager.CreateToastNotifier("App").Show(new ToastNotification(doc));
	}
}
