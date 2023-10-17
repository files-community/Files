using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Files.App.UserControls
{
	public static class ControlFactory
	{
		private static IAppearanceSettingsService appearance = Ioc.Default.GetRequiredService<IAppearanceSettingsService>();

		private static T WithDefaultFont<T>(this T instance) where T : Control
		{
			instance.FontFamily = new FontFamily(appearance.AppThemeFontFamily);
			return instance;
		}

		public static MenuFlyoutItem CreateMenuFlyoutItem()
		{
			return new MenuFlyoutItem().WithDefaultFont();
		}

		public static MenuFlyoutSubItem CreateMenuFlyoutSubItem()
		{
			return new MenuFlyoutSubItem().WithDefaultFont();
		}

		public static AppBarButton CreateAppBarButton()
		{
			return new AppBarButton().WithDefaultFont();
		}
	}
}
