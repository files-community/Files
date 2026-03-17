// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.Graphics;
using Microsoft.UI.Windowing;
using Windows.Win32;

namespace Files.App.Views.Settings
{
	/// <summary>
	/// Creates and manages the toolbar customization window lifecycle.
	/// </summary>
	internal static class ToolbarCustomizationDialog
	{
		private static WindowEx? customizationWindow;

		public static void Show()
		{
			var themeService = Ioc.Default.GetRequiredService<IAppThemeModeService>();
			var window = customizationWindow ??= CreateCustomizationWindow(themeService);
			if (window.Content is Frame frame) frame.RequestedTheme = themeService.AppThemeMode;
			if ((window.Content as Frame)?.Content is ToolbarCustomizationPage page) window.SetTitleBar(page.TitleBarElement);
			themeService.SetAppThemeMode(window, window.AppWindow.TitleBar, themeService.AppThemeMode, callThemeModeChangedEvent: false);

			// Move window to cursor position, matching properties window behavior
			PInvoke.GetCursorPos(out var pointerPosition);
			var displayArea = DisplayArea.GetFromPoint(new PointInt32(pointerPosition.X, pointerPosition.Y), DisplayAreaFallback.Nearest);
			var appWindow = window.AppWindow;
			appWindow.Move(new PointInt32
			{
				X = displayArea.WorkArea.X + Math.Max(0, Math.Min(displayArea.WorkArea.Width - appWindow.Size.Width, pointerPosition.X - displayArea.WorkArea.X)),
				Y = displayArea.WorkArea.Y + Math.Max(0, Math.Min(displayArea.WorkArea.Height - appWindow.Size.Height, pointerPosition.Y - displayArea.WorkArea.Y)),
			});

			window.AppWindow.Show();
			window.Activate();
		}

		private static WindowEx CreateCustomizationWindow(IAppThemeModeService themeService)
		{
			var frame = new Frame { RequestedTheme = themeService.AppThemeMode };
			var window = new WindowEx(460, 400)
			{
				ExtendsContentIntoTitleBar = true,
				IsMaximizable = false,
				Content = frame,
				SystemBackdrop = new AppSystemBackdrop(true),
			};
			window.Closed += (_, _) => customizationWindow = null;
			var appWindow = window.AppWindow;
			appWindow.Title = Strings.CustomizeToolbar.GetLocalizedResource();
			appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
			appWindow.SetIcon(AppLifecycleHelper.AppIconPath);
			frame.Navigate(typeof(ToolbarCustomizationPage), window, new SuppressNavigationTransitionInfo());
			appWindow.Resize(new SizeInt32(
				Math.Max(1, Convert.ToInt32(760 * App.AppModel.AppWindowDPI)),
				Math.Max(1, Convert.ToInt32(560 * App.AppModel.AppWindowDPI))));
			return window;
		}
	}
}
