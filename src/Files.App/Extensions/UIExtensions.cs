using Microsoft.UI.Xaml.Controls;

namespace Files.App.Extensions
{
	public static class UIExtensions
	{
		public static MainPage? GetMainPage(this MainWindow mainWindow)
		{
			return (mainWindow.Content as Frame)?.Content as MainPage;
		}
	}
}
