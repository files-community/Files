namespace Files.App.Extensions
{
	public static class ControlExtensions
	{
		public static MainPage? GetMainPage(this MainWindow mainWindow)
		{
			return mainWindow.RootControl.AppContent as MainPage;
		}
	}
}
