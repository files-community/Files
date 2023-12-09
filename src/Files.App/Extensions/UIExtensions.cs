using Microsoft.UI.Xaml;

namespace Files.App.Extensions
{
	public static class UIExtensions
	{
		public static MainPage? GetMainPage(this MainWindow mainWindow)
		{
			return GetContentRoot(mainWindow) as MainPage;
		}

		public static FrameworkElement? GetContentRoot(this MainWindow mainWindow)
		{
			return mainWindow.MainContent.Content as FrameworkElement;
		}
	}
}
