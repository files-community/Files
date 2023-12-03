using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

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
			return (mainWindow.Content as ContentControl)?.Content as FrameworkElement;
		}
	}
}
