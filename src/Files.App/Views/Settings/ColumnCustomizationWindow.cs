// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Files.App.ViewModels.Settings;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.Graphics;
using Windows.Win32;
using WinRT;

namespace Files.App.Views.Settings
{
	/// <summary>
	/// Represents the navigation parameter of <see cref="ColumnCustomizationPage"/>.
	/// </summary>
	internal sealed partial class ColumnCustomizationNavigationArguments(WindowEx window, ColumnCustomizationViewModel viewModel)
	{
		public WindowEx Window { get; } = window;

		public ColumnCustomizationViewModel ViewModel { get; } = viewModel;
	}

	/// <summary>
	/// Creates the window that customizes the columns of <see cref="DetailsLayoutPage"/>.
	/// </summary>
	internal static class ColumnCustomizationWindow
	{
		private static WindowEx? _window;

		[DynamicWindowsRuntimeCast(typeof(Frame))]
		public static void Show(ColumnCustomizationViewModel viewModel)
		{
			if (App.AppModel.IsMainWindowClosed)
				return;

			_window?.Close();

			var themeService = Ioc.Default.GetRequiredService<IAppThemeModeService>();
			var frame = new Frame { RequestedTheme = themeService.AppThemeMode };
			var window = new WindowEx(460, 400)
			{
				ExtendsContentIntoTitleBar = true,
				IsMaximizable = false,
				Content = frame,
				SystemBackdrop = new AppSystemBackdrop(true),
			};

			void ThemeService_AppThemeModeChanged(object? sender, EventArgs e)
			{
				window.DispatcherQueue.TryEnqueue(() =>
					themeService.SetAppThemeMode(window, window.AppWindow.TitleBar, themeService.AppThemeMode, callThemeModeChangedEvent: false));
			}

			themeService.AppThemeModeChanged += ThemeService_AppThemeModeChanged;
			window.Closed += (_, _) =>
			{
				themeService.AppThemeModeChanged -= ThemeService_AppThemeModeChanged;
				if (_window == window)
					_window = null;
			};

			var appWindow = window.AppWindow;
			appWindow.Title = viewModel.Title;
			appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
			appWindow.SetIcon(AppLifecycleHelper.AppIconPath);
			frame.Navigate(typeof(ColumnCustomizationPage), new ColumnCustomizationNavigationArguments(window, viewModel), new SuppressNavigationTransitionInfo());
			if (frame.Content is ColumnCustomizationPage page)
				window.SetTitleBar(page.TitleBarElement);

			themeService.SetAppThemeMode(window, appWindow.TitleBar, themeService.AppThemeMode, callThemeModeChangedEvent: false);
			appWindow.Resize(new SizeInt32(
				Math.Max(1, Convert.ToInt32(720 * App.AppModel.AppWindowDPI)),
				Math.Max(1, Convert.ToInt32(480 * App.AppModel.AppWindowDPI))));

			PInvoke.GetCursorPos(out var pointerPosition);
			var displayArea = DisplayArea.GetFromPoint(new PointInt32(pointerPosition.X, pointerPosition.Y), DisplayAreaFallback.Nearest);
			appWindow.Move(new PointInt32
			{
				X = displayArea.WorkArea.X + Math.Max(0, Math.Min(displayArea.WorkArea.Width - appWindow.Size.Width, pointerPosition.X - displayArea.WorkArea.X)),
				Y = displayArea.WorkArea.Y + Math.Max(0, Math.Min(displayArea.WorkArea.Height - appWindow.Size.Height, pointerPosition.Y - displayArea.WorkArea.Y)),
			});

			_window = window;
			appWindow.Show();
			window.Activate();
		}
	}
}
